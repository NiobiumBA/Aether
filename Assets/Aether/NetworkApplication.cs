using Aether.Connections;
using Aether.Transports;
using System;

namespace Aether
{
    /// <summary>
    /// Represents this application as a part of network.
    /// </summary>
    public static class NetworkApplication
    {
        /// <summary>
        /// Argument is a new connection.
        /// </summary>
        public static event Action<ConnectionToServer> OnClientConnectionChange;

        public static event Action<ConnectionToClient> OnServerAddConnection;

        /// <summary>
        /// Argument is the new transport.
        /// </summary>
        public static event Action<NetworkTransport> OnTransportChange;

        private static NetworkClientDispatcher s_clientDispatcher;
        private static NetworkServerDispatcher s_serverDispatcher;
        private static NetworkTransport s_activeTransport;

        public static bool IsClient => s_clientDispatcher != null;

        public static bool IsClientOnly => IsClient && !IsServer;

        public static bool ClientConnected => IsClient && ClientDispatcher.Connected;

        public static bool ClientLocalConnected => IsClient && ClientDispatcher.Connection is LocalConnectionToServer;

        public static NetworkClientDispatcher ClientDispatcher => s_clientDispatcher;

        public static bool IsServer => s_serverDispatcher != null;

        public static bool IsServerOnly => IsServer && !IsClient;

        public static NetworkServerDispatcher ServerDispatcher => s_serverDispatcher;

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

            s_clientDispatcher.OnConnectionChange += OnClientConnectionChangeInvoke;
        }

        /// <summary>
        /// Remove ClientDispatcher and disconnect ClientDispatcher.Connection.
        /// </summary>
        public static void DisableClientDispatcher()
        {
            if (IsClient == false)
                return;

            s_clientDispatcher.OnConnectionChange -= OnClientConnectionChangeInvoke;

            s_clientDispatcher.Connection?.Disconnect();

            s_clientDispatcher = null;
        }

        public static void CreateServerDispatcher()
        {
            if (IsServer)
                DisableServerDispatcher();

            s_serverDispatcher = new NetworkServerDispatcher();

            s_serverDispatcher.OnAddConnection += OnServerAddConnectionInvoke;
        }

        /// <summary>
        /// Remove ServerDispatcher and disconnect all connections in ServerDispatcher.
        /// </summary>
        public static void DisableServerDispatcher()
        {
            if (IsServer == false)
                return;

            s_serverDispatcher.OnAddConnection -= OnServerAddConnectionInvoke;

            foreach (NetworkConnection conn in s_serverDispatcher.Connections)
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

            ClientDispatcher.Connection = connectionToServer;
            ServerDispatcher.AddConnection(connectionToClient);
        }

        private static void ActiveTransportOnClientConnect()
        {
            ThrowHelper.ThrowIfNotClient($"{nameof(NetworkTransport)}.{nameof(NetworkTransport.OnClientConnect)}");

            ConnectionToServer conn = new(s_activeTransport);

            s_clientDispatcher.Connection = conn;
        }

        private static void ActiveTransportOnServerConnect(uint connectionId)
        {
            ThrowHelper.ThrowIfNotServer($"{nameof(NetworkTransport)}.{nameof(NetworkTransport.OnServerConnect)}");

            ConnectionToClient conn = new(s_activeTransport, connectionId);

            s_serverDispatcher.AddConnection(conn);
        }

        private static void OnClientConnectionChangeInvoke(ConnectionToServer conn)
        {
            OnClientConnectionChange?.Invoke(conn);
        }

        private static void OnServerAddConnectionInvoke(ConnectionToClient conn)
        {
            OnServerAddConnection?.Invoke(conn);
        }
    }
}
