using Aether.Connections;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aether
{
    /// <summary>
    /// Invoke SendData and HandleData.
    /// Invoke especial methods in all NetworkBehaviours on the scene.
    /// Disconnect when application quit.
    /// </summary>
    public class UnityNetworkEventSystem : SingletonBehaviour<UnityNetworkEventSystem>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ForAllNetworkBehaviours(Action<NetworkBehaviour> action)
        {
            foreach (NetworkIdentity identity in NetworkIdentity.SceneIdentities.Values)
            {
                if (identity.gameObject.activeInHierarchy == false)
                    continue;

                foreach (NetworkBehaviour behaviour in identity.Components)
                {
                    if (behaviour.enabled == false)
                        continue;

                    try
                    {
                        action(behaviour);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, behaviour);
                    }
                }
            }
        }

        [SerializeField] private int m_serverTickRate = 30;

        private bool m_clientStarted = false;
        private bool m_serverStarted = false;

        public virtual int ServerTickRate
        {
            get => m_serverTickRate;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));

                m_serverTickRate = value;
            }
        }

        protected virtual void OnEnable()
        {
            NetworkApplication.OnClientDispatcherCreate += OnClientDispatcherCreate;
            NetworkApplication.OnClientConnectionChange += OnClientConnectionChange;

            NetworkApplication.OnServerDispatcherCreate += OnServerDispatcherCreate;
            NetworkApplication.OnServerAddConnection += OnServerAddConnection;
        }

        protected virtual void OnDisable()
        {
            NetworkApplication.OnClientDispatcherCreate -= OnClientDispatcherCreate;
            NetworkApplication.OnClientConnectionChange -= OnClientConnectionChange;

            NetworkApplication.OnServerDispatcherCreate -= OnServerDispatcherCreate;
            NetworkApplication.OnServerAddConnection -= OnServerAddConnection;
        }

        protected override void Start()
        {
            base.Start();

            ConfigureHeadlessFrameRate();

            if (NetworkApplication.IsClient && m_clientStarted == false)
                ForAllNetworkBehaviours(behaviour => behaviour.ClientStart());

            if (NetworkApplication.IsServer && m_serverStarted == false)
                ForAllNetworkBehaviours(behaviour => behaviour.ServerStart());
        }

        protected virtual void Update()
        {
            if (NetworkApplication.IsClient)
                ForAllNetworkBehaviours(behaviour => behaviour.ClientUpdate());

            if (NetworkApplication.IsServer)
                ForAllNetworkBehaviours(behaviour => behaviour.ServerUpdate());
        }

        protected virtual void LateUpdate()
        {
            NetworkEventSystem.InvokeSendData();

            NetworkEventSystem.InvokeBeforeHandleData();

            NetworkEventSystem.InvokeHandleData();
        }

        protected virtual void FixedUpdate()
        {
            if (NetworkApplication.IsClient)
                ForAllNetworkBehaviours(behaviour => behaviour.ClientFixedUpdate());
        }

        protected virtual void OnApplicationQuit()
        {
            NetworkApplication.DisableClientDispatcher();
            NetworkApplication.DisableServerDispatcher();
        }

        protected virtual void ConfigureHeadlessFrameRate()
        {
#if UNITY_SERVER
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = m_serverTickRate;
#endif
        }

        private void OnClientDispatcherCreate()
        {
            ForAllNetworkBehaviours(behaviour => behaviour.ClientStart());
            m_clientStarted = true;
        }

        private void OnServerDispatcherCreate()
        {
            ForAllNetworkBehaviours(behaviour => behaviour.ServerStart());
            m_serverStarted = true;
        }

        private void OnServerAddConnection(ConnectionToClient conn)
        {
            OnConnect(conn);
        }

        private void OnClientConnectionChange(ConnectionToServer newConn)
        {
            ConnectionToServer currentConnection = NetworkApplication.ClientDispatcher.Connection;

            if (currentConnection != null)
            {
                currentConnection.OnSelfDisconnect -= OnDisconnect;
                currentConnection.OnForcedDisconnect -= OnDisconnect;
            }

            if (newConn != null)
                OnConnect(newConn);
        }

        private void OnConnect(NetworkConnection conn)
        {
            conn.OnSelfDisconnect += OnDisconnect;
            conn.OnForcedDisconnect += OnDisconnect;

            ForAllNetworkBehaviours(behaviour => behaviour.OnConnect(conn));
        }

        private void OnDisconnect(NetworkConnection conn)
        {
            conn.OnSelfDisconnect -= OnDisconnect;
            conn.OnForcedDisconnect -= OnDisconnect;

            ForAllNetworkBehaviours(behaviour => behaviour.OnDisconnect(conn));
        }
    }
}