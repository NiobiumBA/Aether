using Aether.Connections;
using System;
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
        private static bool s_clientStarted = false;
        private static bool s_serverStarted = false;

        [SerializeField] private int m_serverTickRate = 30;

        public virtual int ServerTickRate
        {
            get => m_serverTickRate;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));

                m_serverTickRate = value;

                ConfigureHeadlessFrameRate();
            }
        }

        protected static void ForAllNetworkBehaviours(Action<NetworkBehaviour> action)
        {
            foreach (NetworkIdentity identity in NetworkIdentity.RoomIdentities.Values)
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

        private static void OnClientDispatcherCreate()
        {
            ClientStart();
        }

        private static void OnServerDispatcherCreate()
        {
            ServerStart();
        }

        private static void ClientStart()
        {
            NetworkApplication.ClientDispatcher.RegisterHandler(NetworkBehaviour.DataHandlerName, NetworkBehaviour.DataHandler);

            s_clientStarted = true;
        }

        private static void ServerStart()
        {
            NetworkApplication.ServerDispatcher.RegisterHandler(NetworkBehaviour.DataHandlerName, NetworkBehaviour.DataHandler);

            s_serverStarted = true;
        }

        private static void OnServerAddConnection(ConnectionToClient conn)
        {
            OnConnectMethod(conn);
        }

        private static void OnClientConnectionChange(ConnectionToServer newConn)
        {
            ConnectionToServer currentConnection = NetworkApplication.ClientDispatcher.Connection;

            if (currentConnection != null)
            {
                currentConnection.OnSelfDisconnect -= OnDisconnectMethod;
                currentConnection.OnForcedDisconnect -= OnDisconnectMethod;
            }

            if (newConn != null)
                OnConnectMethod(newConn);
        }

        private static void OnConnectMethod(NetworkConnection conn)
        {
            conn.OnSelfDisconnect += OnDisconnectMethod;
            conn.OnForcedDisconnect += OnDisconnectMethod;

            ForAllNetworkBehaviours(behaviour => behaviour.OnConnect(conn));
        }

        private static void OnDisconnectMethod(NetworkConnection conn)
        {
            conn.OnSelfDisconnect -= OnDisconnectMethod;
            conn.OnForcedDisconnect -= OnDisconnectMethod;

            ForAllNetworkBehaviours(behaviour => behaviour.OnDisconnect(conn));
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

            if (ShouldBeDestroyed)
                return;

            ConfigureHeadlessFrameRate();

            if (NetworkApplication.IsClient && s_clientStarted == false)
                ClientStart();

            if (NetworkApplication.IsServer && s_serverStarted == false)
                ServerStart();
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
            Time.fixedDeltaTime = 1f / m_serverTickRate;
#endif
        }
    }
}