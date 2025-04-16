using Aether.Connections;
using System.Collections.Generic;
using System;
using Aether.Messages;

namespace Aether
{
    public class NetworkServerDispatcher : NetworkDispatcher
    {
        private readonly List<ConnectionToClient> m_connections = new();

        public IReadOnlyList<ConnectionToClient> Connections => m_connections;

        public NetworkServerDispatcher()
        {
        }

        /// <summary>
        /// Add connection.
        /// If you want to remove one of Connections, disconnect it.
        /// </summary>
        public void AddConnection(ConnectionToClient connection)
        {
            ThrowHelper.ThrowIfNull(connection, nameof(connection));

            if (connection.IsActive == false)
                ThrowHelper.ArgumentInactiveConnection(nameof(connection));

            if (m_connections.Contains(connection))
                throw new ArgumentException($"Connection has already been added", nameof(connection));

            m_connections.Add(connection);

            connection.HandleData += ProcessDataHandlers;
            connection.OnForcedDisconnect += RemoveConnection;
            connection.OnSelfDisconnect += RemoveConnection;
        }

        public void SendAll(string handlerName, ArraySegment<byte> data)
        {
            foreach (ConnectionToClient conn in Connections)
                SendByConnection(conn, handlerName, data);
        }

        public void SendMessageAll<TMessage>(TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            foreach (ConnectionToClient conn in Connections)
                SendMessageByConnection(conn, message);
        }

        /// <summary>
        /// Send data all connections exclude LocalConnectionToClient.
        /// </summary>
        public void SendAllRemote(string handlerName, ArraySegment<byte> data)
        {
            foreach (ConnectionToClient conn in Connections)
            {
                if (conn is not LocalConnectionToClient)
                    SendByConnection(conn, handlerName, data);
            }
        }

        /// <summary>
        /// Send message all connections exclude LocalConnectionToClient.
        /// </summary>
        public void SendMessageAllRemote<TMessage>(TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            foreach (NetworkConnection conn in Connections)
            {
                if (conn is not LocalConnectionToClient)
                    SendMessageByConnection(conn, message);
            }
        }

        private void RemoveConnection(NetworkConnection connection)
        {
            m_connections.Remove(connection as ConnectionToClient);

            connection.HandleData -= ProcessDataHandlers;
            connection.OnForcedDisconnect -= RemoveConnection;
            connection.OnSelfDisconnect -= RemoveConnection;
        }
    }
}
