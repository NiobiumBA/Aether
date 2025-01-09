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
        /// <summary>
        /// TKey: handler name
        /// </summary>
        private readonly Dictionary<string, NetworkDataHandler> m_dataHandlers = new();

        public static void SendByConnection(NetworkConnection connection, string handlerName, ArraySegment<byte> data)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteString(handlerName);
            writer.WriteBytes(data);

            ArraySegment<byte> processedData = writer.ToArraySegment();

            connection.Send(processedData);

            NetworkDiagnostics.SentDataRecord.TryAdd(handlerName, processedData.Count);
        }

        public static void SendMessageByConnection<TMessage>(NetworkConnection connection, TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteString(handlerName);
            writer.WriteMessage(message);

            ArraySegment<byte> processedData = writer.ToArraySegment();

            connection.Send(processedData);

            NetworkDiagnostics.SentDataRecord.TryAdd(handlerName, processedData.Count);
        }

        protected NetworkDispatcher()
        {
        }

        public void RegisterDataHandler(string handlerName, NetworkDataHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

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

            //if (m_dataHandlers.Remove(handlerName) == false)
            //    throw new ArgumentException($"Could not be found a data handler with name: {handlerName}.");
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

                try
                {
                    handlerName = reader.ReadString();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);

                    connection.Disconnect();

                    return;
                }

                int startReaderPosition = reader.Position;

                if (m_dataHandlers.TryGetValue(handlerName, out NetworkDataHandler dataHandler) == false)
                {
                    connection.Disconnect();

                    Debug.LogError($"Could not be found a data handler with name: {handlerName}");
                    return;
                }

                try
                {
                    dataHandler(connection, reader);
                }
                catch (Exception exception)
                {
                    connection.Disconnect();

                    Debug.LogError($"{exception}\nin handler with name: {handlerName}");
                    return;
                }

                int currentHandledDataSize = reader.Position - startReaderPosition;

                NetworkDiagnostics.HandledDataRecord.TryAdd(handlerName, currentHandledDataSize);
            }
        }
    }
}
