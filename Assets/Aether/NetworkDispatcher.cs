using Aether.Messages;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aether
{
    public delegate void NetworkDataHandler(NetworkConnection connection, NetworkReader reader);
    public delegate void NetworkMessageCallback<TMessage>(NetworkConnection connection, TMessage message)
        where TMessage : unmanaged, INetworkMessage;

    public abstract class NetworkDispatcher
    {
        public static void SendByConnection(NetworkConnection connection, string handlerName, ArraySegment<byte> data)
        {
            ThrowHelper.ThrowIfNull(connection, nameof(connection));

            if(string.IsNullOrEmpty(handlerName))
                throw new ArgumentException(nameof(handlerName));

            if (connection.IsActive == false)
                ThrowHelper.ArgumentInactiveConnection(nameof(connection));

            SendByConnection(connection, handlerName, data.Count, (writer) => writer.WriteBytes(data));
        }

        public static void SendMessageByConnection<TMessage>(NetworkConnection connection, TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            ThrowHelper.ThrowIfNull(connection, nameof(connection));

            if (connection.IsActive == false)
                ThrowHelper.ArgumentInactiveConnection(nameof(connection));

            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();
            int dataSize = MessageHandling.GetMessageSize<TMessage>();
            
            SendByConnection(connection, handlerName, dataSize, (writer) => writer.WriteMessage(message));
        }

        private static void SendByConnection(NetworkConnection connection, string handlerName, int dataSize, Action<NetworkWriter> writeAction)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteString(handlerName);
            writer.WriteInt(dataSize);
            writeAction(writer);

            ArraySegment<byte> processedData = writer.ToArraySegment();

            connection.Send(processedData);

            NetworkDiagnostics.SentDataRecord.TryAdd(handlerName, processedData.Count);
        }

        // TKey: handler name
        private readonly Dictionary<string, NetworkDataHandler> m_dataHandlers = new();

        protected NetworkDispatcher()
        {
        }

        public void RegisterHandler(string handlerName, NetworkDataHandler handler)
        {
            ThrowHelper.ThrowIfNull(handler, nameof(handler));

            if (string.IsNullOrEmpty(handlerName))
                throw new ArgumentException(nameof(handlerName));

            if (m_dataHandlers.ContainsKey(handlerName))
            {
                ThrowHelper.RepeatedHandlerRegister(handlerName);
            }

            m_dataHandlers[handlerName] = handler;
        }

        public void RegisterMessageCallback<TMessage>(NetworkMessageCallback<TMessage> callback)
            where TMessage : unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            if (m_dataHandlers.ContainsKey(handlerName))
            {
                ThrowHelper.RepeatedMessageRegister(handlerName);
            }

            m_dataHandlers[handlerName] = MessageHandling.GetMessageHandler<TMessage>(callback);
        }

        public bool RemoveHandler(string handlerName)
        {
            return m_dataHandlers.Remove(handlerName);
        }

        public bool RemoveMessageCallback<TMessage>()
            where TMessage : unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            return RemoveHandler(handlerName);
        }

        protected void ProcessDataHandlers(NetworkConnection connection, ArraySegment<byte> data)
        {
            NetworkReader reader = new(data);

            while (reader.Remaining > 0 && connection.IsActive)
            {
                string handlerName;
                int dataSize;

                int startReaderPosition = reader.Position;

                try
                {
                    handlerName = reader.ReadString();
                    dataSize = reader.ReadInt();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    connection.Disconnect();
                    return;
                }

                if (m_dataHandlers.TryGetValue(handlerName, out NetworkDataHandler dataHandler) == false)
                {
                    Debug.LogError($"Could not be found a data handler with name: {handlerName}");
                    connection.Disconnect();
                    return;
                }

                NetworkReader localReader = new(reader.ReadBytes(dataSize));

                try
                {
                    dataHandler(connection, localReader);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Caught exception in handler with name: {handlerName}\n{exception}");
                    connection.Disconnect();
                    return;
                }

                int currentHandledDataSize = reader.Position - startReaderPosition;

                NetworkDiagnostics.HandledDataRecord.TryAdd(handlerName, currentHandledDataSize);
            }
        }
    }
}
