﻿using Aether.Connections;
using System;
using System.Linq;
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
            NetworkApplication.OnClientConnectionChange += OnClientConnectionChange;
            NetworkApplication.OnServerAddConnection += OnServerAddConnection;
        }

        protected virtual void OnDisable()
        {
            NetworkApplication.OnClientConnectionChange -= OnClientConnectionChange;
            NetworkApplication.OnServerAddConnection -= OnServerAddConnection;
        }

        protected override void Start()
        {
            base.Start();

            ConfigureHeadlessFrameRate();
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
            DisconnectAll();
        }

        protected virtual void ConfigureHeadlessFrameRate()
        {
#if UNITY_SERVER
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = m_serverTickRate;
#endif
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

        private void DisconnectAll()
        {
            NetworkApplication.ClientDispatcher?.Connection?.Disconnect();

            if (NetworkApplication.IsServer)
            {
                // Use ToArray to copy the collection
                ConnectionToClient[] connections = NetworkApplication.ServerDispatcher.Connections.ToArray();

                foreach (ConnectionToClient conn in connections)
                    conn.Disconnect();
            }
        }
    }
}