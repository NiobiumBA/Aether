using Aether.Messages;
using Aether.Synchronization;
using System;
using System.Collections;
using UnityEngine;

namespace Aether
{
    // TODO Add mode where client is owner, server is observer
    [DisallowMultipleComponent]
    public class NetworkTransform : NetworkBehaviour
    {
        public enum SyncMethod
        {
            ClientUpdate, ClientFixedUpdate
        }

        private struct TransformInfo : INetworkMessage, IEquatable<TransformInfo>
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;

            public readonly bool Equals(TransformInfo other)
            {
                return position == other.position &&
                    rotation == other.rotation &&
                    scale == other.scale;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(position, rotation, scale);
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

        [SerializeField] private SyncMethod m_clientSyncMethod;
        [SerializeField] private float m_syncTime;
        [SerializeField] private float m_positionSmoothTime;
        [SerializeField] private float m_rotationSmoothTime;
        [SerializeField] private float m_scaleSmoothTime;

        private SyncValue<TransformInfo> m_syncInfo;

        private TransformInfo m_lastInfo;
        private bool m_firstReceiveMessage = true;

        private Vector3 m_positionVelocity;
        private Vector3 m_rotationVelocity;
        private Vector3 m_scaleVelocity;

        public SyncMethod ClientSyncMethod
        {
            get => m_clientSyncMethod;
            set => m_clientSyncMethod = value;
        }

        /// <summary>
        /// The minimum time between sending data of changing transform.
        /// </summary>
        public float SyncTime
        {
            get => m_syncTime;
            set
            {
                m_syncTime = Mathf.Max(value, 0);
            }
        }

        public float PositionSmoothTime
        {
            get => m_positionSmoothTime;
            set
            {
                m_positionSmoothTime = Mathf.Max(value, 0);
            }
        }

        public float RotationSmoothTime
        {
            get => m_rotationSmoothTime;
            set
            {
                m_rotationSmoothTime = Mathf.Max(value, 0);
            }
        }

        public float ScaleSmoothTime
        {
            get => m_scaleSmoothTime;
            set
            {
                m_scaleSmoothTime = Mathf.Max(value, 0);
            }
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
            m_lastInfo = GetTransformMessage();

            StartCoroutine(SyncCoroutine());
        }

        private void Awake()
        {
            m_syncInfo = new SyncValue<TransformInfo>(this, SyncMode.ServerOwner, OnTransformChange);
        }

        private TransformInfo OnTransformChange(TransformInfo value)
        {
            if (NetworkApplication.IsServer)
                return value;

            if (m_firstReceiveMessage == false)
                return value;

            transform.SetPositionAndRotation(value.position, Quaternion.Euler(value.rotation));
            transform.localScale = value.scale;

            m_firstReceiveMessage = false;

            return value;
        }

        private void Start()
        {
            if (NetworkApplication.IsServer)
                m_syncInfo.Value = m_lastInfo;
        }

        private void OnDestroy()
        {
            m_syncInfo?.Dispose();
        }

        protected internal override void ClientUpdate()
        {
            if (m_clientSyncMethod == SyncMethod.ClientUpdate)
                UpdateTransform(Time.deltaTime);
        }

        protected internal override void ClientFixedUpdate()
        {
            if (m_clientSyncMethod == SyncMethod.ClientFixedUpdate)
                UpdateTransform(Time.fixedDeltaTime);
        }

        private void UpdateTransform(float deltaTime)
        {
            if (NetworkApplication.IsServer)
                return;

            Vector3 currentPos = transform.position;
            Vector3 currentRotation = transform.eulerAngles;
            Vector3 currentScale = transform.localScale;

            Vector3 targetPos = m_syncInfo.Value.position;
            Vector3 targetRotation = m_syncInfo.Value.rotation;
            Vector3 targetScale = m_syncInfo.Value.scale;

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
                if (NetworkApplication.IsServer)
                {
                    TransformInfo currentMessage = GetTransformMessage();

                    if (currentMessage != m_lastInfo)
                    {
                        m_syncInfo.Value = currentMessage;
                        m_lastInfo = currentMessage;

                        yield return new WaitForSeconds(SyncTime);
                    }
                    else
                    {
                        yield return null;
                    }
                }

                yield return null;
            }
        }

        private TransformInfo GetTransformMessage()
        {
            return new TransformInfo()
            {
                position = transform.position,
                rotation = transform.rotation.eulerAngles,
                scale = transform.localScale
            };
        }
    }
}
