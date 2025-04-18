using Aether.Connections;
using Aether.Messages;
using Aether.Synchronization;
using System;
using System.Collections;
using UnityEngine;

namespace Aether
{
    [DisallowMultipleComponent]
    public class NetworkTransform : NetworkBehaviour
    {
        public enum UpdateMethodType
        {
            Update, FixedUpdate
        }

        public readonly struct TransformInfo : INetworkMessage, IEquatable<TransformInfo>
        {
            private readonly Vector3 m_position;
            private readonly Vector3 m_rotation;
            private readonly Vector3 m_scale;

            public readonly Vector3 Position => m_position;
            public readonly Vector3 Rotation => m_rotation;
            public readonly Vector3 Scale => m_scale;

            public TransformInfo(Transform transform)
            {
                m_position = transform.position;
                m_rotation = transform.eulerAngles;
                m_scale = transform.localScale;
            }

            public readonly bool Equals(TransformInfo other)
            {
                return m_position == other.m_position &&
                    m_rotation == other.m_rotation &&
                    m_scale == other.m_scale;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(m_position, m_rotation, m_scale);
            }

            public override readonly bool Equals(object obj)
            {
                return obj is TransformInfo other && this == other;
            }

            public static bool operator ==(TransformInfo left, TransformInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(TransformInfo left, TransformInfo right)
            {
                return !left.Equals(right);
            }
        }

        public event Action<TransformInfo> Changed;

        [SerializeField] private UpdateMethodType m_observingMethod;
        [SerializeField] private SyncMode m_syncMode;
        [SerializeField] private float m_syncTime;
        [SerializeField] private float m_positionSmoothTime;
        [SerializeField] private float m_rotationSmoothTime;
        [SerializeField] private float m_scaleSmoothTime;

        private SyncValue<TransformInfo> m_syncInfo;

        private TransformInfo m_lastInfo;
        private ConnectionToClient m_ownerClient;
        private SyncMode? m_runtimeSyncMode;

        private Vector3 m_positionVelocity;
        private Vector3 m_rotationVelocity;
        private Vector3 m_scaleVelocity;

        public UpdateMethodType ObservingMethod
        {
            get => m_observingMethod;
            set => m_observingMethod = value;
        }

        public SyncMode Mode => m_runtimeSyncMode.GetValueOrDefault(m_syncMode);

        public ConnectionToClient OwnerClient
        {
            get
            {
                if (Mode != SyncMode.ClientOwner)
                    ThrowHelper.ShouldUseWithSyncMode(nameof(OwnerClient), SyncMode.ClientOwner);

                return m_ownerClient;
            }
            set
            {
                if (Mode != SyncMode.ClientOwner)
                    ThrowHelper.ShouldUseWithSyncMode(nameof(OwnerClient), SyncMode.ClientOwner);

                m_ownerClient = value;

                if (m_syncInfo != null)
                    UpdateSyncInfoOwners();
            }
        }

        /// <summary>
        /// The minimum time between sending data of changing transform.
        /// </summary>
        public float SyncTime
        {
            get => m_syncTime;
            set => m_syncTime = Mathf.Max(value, 0);
        }

        public float PositionSmoothTime
        {
            get => m_positionSmoothTime;
            set => m_positionSmoothTime = Mathf.Max(value, 0);
        }

        public float RotationSmoothTime
        {
            get => m_rotationSmoothTime;
            set => m_rotationSmoothTime = Mathf.Max(value, 0);
        }

        public float ScaleSmoothTime
        {
            get => m_scaleSmoothTime;
            set => m_scaleSmoothTime = Mathf.Max(value, 0);
        }

        private static Vector3 SmoothDampRotation(Vector3 current,
                                                  Vector3 target,
                                                  ref Vector3 currentVelocity,
                                                  float smoothTime,
                                                  float deltaTime)
        {
            float currentVelocityX = currentVelocity.x;
            float currentVelocityY = currentVelocity.y;
            float currentVelocityZ = currentVelocity.z;

            float resultX = Mathf.SmoothDampAngle(current.x, target.x, ref currentVelocityX, smoothTime, float.MaxValue, deltaTime);
            float resultY = Mathf.SmoothDampAngle(current.y, target.y, ref currentVelocityY, smoothTime, float.MaxValue, deltaTime);
            float resultZ = Mathf.SmoothDampAngle(current.z, target.z, ref currentVelocityZ, smoothTime, float.MaxValue, deltaTime);

            currentVelocity = new Vector3(currentVelocityX, currentVelocityY, currentVelocityZ);

            return new Vector3(resultX, resultY, resultZ);
        }

        private void OnEnable()
        {
            m_lastInfo = new TransformInfo(transform);

            StartCoroutine(SyncCoroutine());
        }

        private void Awake()
        {
            m_runtimeSyncMode = m_syncMode;

            m_syncInfo = new SyncValue<TransformInfo>(this, Mode, new TransformInfo(transform), OnTransformChange);

            if (NetworkApplication.IsServer && Mode == SyncMode.ClientOwner)
                UpdateSyncInfoOwners();
        }

        private void Start()
        {
            if (IsSendInfo())
                m_syncInfo.Value = m_lastInfo;
        }

        private void OnDestroy()
        {
            m_syncInfo?.Dispose();
        }

        protected internal override void ClientUpdate()
        {
            if (!NetworkApplication.IsServer && Mode == SyncMode.ServerOwner && ObservingMethod == UpdateMethodType.Update)
                UpdateTransform(Time.deltaTime);
        }

        protected internal override void ClientFixedUpdate()
        {
            if (!NetworkApplication.IsServer && Mode == SyncMode.ServerOwner && ObservingMethod == UpdateMethodType.FixedUpdate)
                UpdateTransform(Time.fixedDeltaTime);
        }

        protected internal override void ServerUpdate()
        {
            if (!NetworkApplication.IsClient && Mode == SyncMode.ClientOwner && ObservingMethod == UpdateMethodType.Update)
                UpdateTransform(Time.deltaTime);
        }

        protected internal override void ServerFixedUpdate()
        {
            if (!NetworkApplication.IsClient && Mode == SyncMode.ClientOwner && ObservingMethod == UpdateMethodType.FixedUpdate)
                UpdateTransform(Time.fixedDeltaTime);
        }

        private void UpdateSyncInfoOwners()
        {
            m_syncInfo.OwnerConnections.Clear();

            if (m_ownerClient != null)
                m_syncInfo.OwnerConnections.Add(m_ownerClient);
        }

        private TransformInfo OnTransformChange(TransformInfo value)
        {
            Changed?.Invoke(value);
            return value;
        }

        private void UpdateTransform(float deltaTime)
        {
            Vector3 currentPos = transform.position;
            Vector3 currentRotation = transform.eulerAngles;
            Vector3 currentScale = transform.localScale;

            Vector3 targetPos = m_syncInfo.Value.Position;
            Vector3 targetRotation = m_syncInfo.Value.Rotation;
            Vector3 targetScale = m_syncInfo.Value.Scale;

            Vector3 resultPos = Vector3.SmoothDamp(
                currentPos, targetPos, ref m_positionVelocity, m_positionSmoothTime, float.MaxValue, deltaTime);
            
            Vector3 resultRotation = SmoothDampRotation(
                currentRotation, targetRotation, ref m_rotationVelocity, m_rotationSmoothTime, deltaTime);
            
            Vector3 resultScale = Vector3.SmoothDamp(
                currentScale, targetScale, ref m_scaleVelocity, m_scaleSmoothTime, float.MaxValue, deltaTime);

            transform.SetPositionAndRotation(resultPos, Quaternion.Euler(resultRotation));
            transform.localScale = resultScale;
        }

        private IEnumerator SyncCoroutine()
        {
            while (isActiveAndEnabled)
            {
                if (IsSendInfo())
                {
                    TransformInfo currentInfo = new(transform);

                    if (currentInfo != m_lastInfo)
                    {
                        m_syncInfo.Value = currentInfo;
                        m_lastInfo = currentInfo;

                        yield return new WaitForSeconds(SyncTime);
                    }
                    else
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }

        private bool IsSendInfo()
        {
            bool shouldSendOnServer = NetworkApplication.IsServer && Mode == SyncMode.ServerOwner;
            bool shouldSendOnClient = NetworkApplication.IsClient && Mode == SyncMode.ClientOwner;
            return shouldSendOnServer || shouldSendOnClient;
        }
    }
}
