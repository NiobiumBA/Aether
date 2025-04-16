using Aether.Connections;
using Aether.Transports;
using System;
using System.Linq;

namespace Aether
{
    /// <summary>
    /// Represents this application as a part of network.
    /// </summary>
    public static class NetworkApplication
    {
        public static event Action OnClientDispatcherCreate;

        /// <summary>
        /// Argument is a new connection.
        /// </summary>
        public static event Action<ConnectionToServer> OnClientConnectionChange;

        public static event Action OnServerDispatcherCreate;

        public static event Action<ConnectionToClient> OnServerAddConnection;

        /// <summary>
        /// Argument is the new transport.
        /// </summary>
        public static event Action<NetworkTransport> OnTransportChange;

        private static NetworkClientDispatcher s_clientDispatcher;
        private static NetworkServerDispatcher s_serverDispatcher;
        private static NetworkTransport s_activeTransport;

        // --- Client
        public static bool IsClient => s_clientDispatcher != null;

        public static bool IsClientOnly => IsClient && !IsServer;

        public static bool ClientConnected => IsClient && ClientDispatcher.Connected;

        public static bool ClientLocalConnected => IsClient && ClientDispatcher.Connection is LocalConnectionToServer;

        public static NetworkClientDispatcher ClientDispatcher => s_clientDispatcher;
        // ---


        // --- Server
        public static bool IsServer => s_serverDispatcher != null;

        public static bool IsServerOnly => IsServer && !IsClient;

        public static NetworkServerDispatcher ServerDispatcher => s_serverDispatcher;
        // ---

        /// <summary>
        /// Connections are monitored through ActiveTransport and link them with dispatchers.
        /// </summary>
        public static NetworkTransport ActiveTransport
        {
            get => s_activeTransport;
            set
            {
                if (s_activeTransport == value) return;

                if (s_activeTransport != null)
                {
                    s_activeTransport.OnClientConnect -= ActiveTransportOnClientConnect;
                    s_activeTransport.OnServerConnect -= ActiveTransportOnServerConnect;
                }

                OnTransportChange?.Invoke(value);

                s_activeTransport = value;

                if (s_activeTransport != null)
                {
                    s_activeTransport.OnClientConnect += ActiveTransportOnClientConnect;
                    s_activeTransport.OnServerConnect += ActiveTransportOnServerConnect;
                }
            }
        }

        public static void CreateClientDispatcher()
        {
            if (IsClient)
                DisableClientDispatcher();

            s_clientDispatcher = new NetworkClientDispatcher();

            OnClientDispatcherCreate?.Invoke();
        }

        /// <summary>
        /// Remove ClientDispatcher and disconnect ClientDispatcher.Connection.
        /// </summary>
        public static void DisableClientDispatcher()
        {
            if (IsClient == false)
                return;

            s_clientDispatcher.Connection?.Disconnect();

            s_clientDispatcher = null;
        }

        public static void CreateServerDispatcher()
        {
            if (IsServer)
                DisableServerDispatcher();

            s_serverDispatcher = new NetworkServerDispatcher();

            OnServerDispatcherCreate?.Invoke();
        }

        /// <summary>
        /// Remove ServerDispatcher and disconnect all connections in ServerDispatcher.
        /// </summary>
        public static void DisableServerDispatcher()
        {
            if (IsServer == false)
                return;

            ConnectionToClient[] copyConnections = s_serverDispatcher.Connections.ToArray();

            foreach (NetworkConnection conn in copyConnections)
            {
                conn.Disconnect();
            }

            s_serverDispatcher = null;
        }

        public static void CreateLocalConnection(uint connectionId = 0)
        {
            ThrowHelper.ThrowIfNotClient(nameof(CreateLocalConnection));
            ThrowHelper.ThrowIfNotServer(nameof(CreateLocalConnection));

            LocalTransport transport = new(connectionId);

            LocalConnectionToClient connectionToClient = new(transport);
            LocalConnectionToServer connectionToServer = new(transport);

            transport.ClientConnect(null);

            s_clientDispatcher.Connection = connectionToServer;
            s_serverDispatcher.AddConnection(connectionToClient);
        }

        private static void ActiveTransportOnClientConnect()
        {
            ThrowHelper.ThrowIfNotClient($"{nameof(NetworkTransport)}.{nameof(NetworkTransport.OnClientConnect)}");

            ConnectionToServer conn = new(s_activeTransport);

            s_clientDispatcher.Connection = conn;

            OnClientConnectionChange?.Invoke(conn);
        }

        private static void ActiveTransportOnServerConnect(uint connectionId)
        {
            ThrowHelper.ThrowIfNotServer($"{nameof(NetworkTransport)}.{nameof(NetworkTransport.OnServerConnect)}");

            ConnectionToClient conn = new(s_activeTransport, connectionId);

            s_serverDispatcher.AddConnection(conn);

            OnServerAddConnection?.Invoke(conn);

            UnityEngine.Debug.Log("ActiveTransportOnServerConnect");
        }
    }
}
