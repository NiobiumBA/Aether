using Aether.Connections;
using Aether.Messages;
using System;

namespace Aether
{
    public class NetworkClientDispatcher : NetworkDispatcher
    {
        private ConnectionToServer m_connection;

        /// <summary>
        /// If you change to new connection, it will disconnect previous.
        /// If connection is disconnected, Connection will set to null.
        /// </summary>
        /// <exception cref="ArgumentException"> Value is not active. </exception>
        public ConnectionToServer Connection
        {
            get => m_connection;
            set
            {
                if (m_connection == value) return;

                if (value != null && value.IsActive == false)
                    ThrowHelper.ArgumentInactiveConnection(nameof(value));

                if (m_connection != null)
                {
                    m_connection.HandleData -= ProcessDataHandlers;
                    m_connection.OnSelfDisconnect -= ResetConnection;
                    m_connection.OnForcedDisconnect -= ResetConnection;

                    m_connection.Disconnect();
                }

                m_connection = value;

                if (m_connection != null)
                {
                    m_connection.HandleData += ProcessDataHandlers;
                    m_connection.OnSelfDisconnect += ResetConnection;
                    m_connection.OnForcedDisconnect += ResetConnection;
                }
            }
        }

        public bool Connected => Connection != null && Connection.IsActive;

        public NetworkClientDispatcher()
        {
        }

        public void RegisterHandler(string handlerName, Action<NetworkReader> handler)
        {
            RegisterHandler(handlerName, (conn, data) => handler(data));
        }

        public void RegisterMessageCallback<TMessage>(Action<TMessage> callback)
            where TMessage : unmanaged, INetworkMessage
        {
            RegisterMessageCallback<TMessage>((conn, message) => callback(message));
        }

        public void Send(string handlerName, ArraySegment<byte> data)
        {
            if (Connection == null)
                throw new NullReferenceException(nameof(Connection));

            SendByConnection(m_connection, handlerName, data);
        }

        public void SendMessage<TMessage>(TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            if (Connection == null)
                throw new NullReferenceException(nameof(Connection));

            SendMessageByConnection(m_connection, message);
        }

        private void ResetConnection(NetworkConnection conn)
        {
            m_connection = null;
        }
    }
}
