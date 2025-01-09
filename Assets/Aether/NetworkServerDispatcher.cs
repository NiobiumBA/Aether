using Aether.Connections;
using System.Collections.Generic;
using System;
using Aether.Messages;

namespace Aether
{
    public class NetworkServerDispatcher : NetworkDispatcher
    {
        public event Action<ConnectionToClient> OnAddConnection;

        private readonly List<ConnectionToClient> m_connections = new();

        public IReadOnlyList<ConnectionToClient> Connections => m_connections;

        public NetworkServerDispatcher()
        {
        }

        /// <summary>
        /// Add connection.
        /// If you want to remove one of Connections, disconnect it.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"> This connection is not active or has already been added. </exception>
        public void AddConnection(ConnectionToClient connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (connection.IsActive == false)
                ThrowHelper.ArgumentInactiveConnection(nameof(connection));

            if (m_connections.Contains(connection))
                throw new ArgumentException($"{connection} has already been added", nameof(connection));

            m_connections.Add(connection);

            connection.HandleData += ProcessDataHandlers;
            connection.OnForcedDisconnect += RemoveConnection;
            connection.OnSelfDisconnect += RemoveConnection;

            OnAddConnection?.Invoke(connection);
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
