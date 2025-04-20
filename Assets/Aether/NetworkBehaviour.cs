using Aether.Connections;
using Aether.Messages;
using Aether.Messages.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aether
{
    /// <summary>
    /// Contains virtual methods which invoking by events.
    /// Send and receive data to another NetworkBehaviours
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        internal const string DataHandlerName = "BehaviourHandler";

        private readonly Dictionary<ushort, NetworkDataHandler> m_dataHandlers = new();

        public NetworkIdentity Identity { get; internal set; }
        public byte ComponentId { get; internal set; }

        internal static void DataHandler(NetworkConnection connection, NetworkReader reader)
        {
            NetworkBehaviour behaviour;
            ushort handlerId;

            try
            {
                behaviour = reader.ReadNetworkBehaviour();
                handlerId = reader.ReadUShort();
            }
            catch (Exception ex)
            {
                throw new InvalidNetworkDataException("Failed to read component and handler name", ex);
            }

            if (behaviour.m_dataHandlers.TryGetValue(handlerId, out var handler) == false)
                throw new Exception($"Could not be found a data handler with name: {handlerId}");

            try
            {
                handler(connection, reader);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Caught exception in object: {behaviour} in handler with name: {handlerId}\n" +
                               $"{ex}");
                connection.Disconnect();
            }
        }

        protected void RegisterDataHandler(string handlerName, NetworkDataHandler handler)
        {
            ThrowHelper.ThrowIfNull(handler, nameof(handlerName));

            if (string.IsNullOrEmpty(handlerName))
                throw new ArgumentException(nameof(handlerName));

            ushort handlerId = StableHash.GetHash16(handlerName);

            if (m_dataHandlers.ContainsKey(handlerId))
            {
                ThrowHelper.RepeatedHandlerRegister(handlerName);
            }

            m_dataHandlers[handlerId] = handler;
        }

        protected void RegisterDataHandler(string handlerName, Action<NetworkReader> handler)
        {
            RegisterDataHandler(handlerName, (conn, reader) => handler(reader));
        }

        protected void RegisterMessageCallback<TMessage>(NetworkMessageCallback<TMessage> callback)
            where TMessage : unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            ushort handlerId = StableHash.GetHash16(handlerName);

            if (m_dataHandlers.ContainsKey(handlerId))
            {
                ThrowHelper.RepeatedMessageRegister(handlerName);
            }

            m_dataHandlers[handlerId] = MessageHandling.GetMessageHandler<TMessage>(callback);
        }

        protected void RegisterMessageCallback<TMessage>(Action<TMessage> callback)
            where TMessage: unmanaged, INetworkMessage
        {
            RegisterMessageCallback<TMessage>((conn, message) => callback(message));
        }

        protected bool RemoveDataHandler(string handlerName)
        {
            ushort handlerId = StableHash.GetHash16(handlerName);

            return m_dataHandlers.Remove(handlerId);
        }

        protected bool RemoveMessageCallback<TMessage>()
            where TMessage: unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            return RemoveDataHandler(handlerName);
        }

        protected void SendData(NetworkConnection connection, string handlerName, ArraySegment<byte> data)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            WriteBehaviourData(writer, handlerName);
            writer.WriteBytes(data);

            NetworkDispatcher.SendByConnection(connection, DataHandlerName, writer.ToArraySegment());
        }

        protected void SendDataToServer(string handlerName, ArraySegment<byte> data)
        {
            ThrowHelper.ThrowIfNotClient(nameof(SendDataToServer));

            SendData(NetworkApplication.ClientDispatcher.Connection, handlerName, data);
        }

        protected void SendDataToClientsAll(string handlerName, ArraySegment<byte> data)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SendDataToClientsAll));

            foreach (ConnectionToClient conn in NetworkApplication.ServerDispatcher.Connections)
            {
                SendData(conn, handlerName, data);
            }
        }

        protected void SendMessage<TMessage>(NetworkConnection connection, TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            WriteBehaviourData(writer, handlerName);
            writer.WriteMessage(message);

            NetworkDispatcher.SendByConnection(connection, DataHandlerName, writer.ToArraySegment());
        }

        protected void SendMessageToServer<TMessage>(TMessage message)
            where TMessage: unmanaged, INetworkMessage
        {
            ThrowHelper.ThrowIfNotClient(nameof(SendMessageToServer));

            SendMessage(NetworkApplication.ClientDispatcher.Connection, message);
        }

        protected void SendMessageToClientsAll<TMessage>(TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            ThrowHelper.ThrowIfNotServer(nameof(SendMessageToClientsAll));

            foreach (ConnectionToClient conn in NetworkApplication.ServerDispatcher.Connections)
            {
                SendMessage(conn, message);
            }
        }

        private void WriteBehaviourData(NetworkWriter writer, string handlerName)
        {
            ushort handlerId = StableHash.GetHash16(handlerName);

            writer.WriteNetworkBehaviour(this);
            writer.WriteUShort(handlerId);
        }

        protected internal virtual void OnConnect(NetworkConnection connection) { }

        protected internal virtual void OnDisconnect(NetworkConnection connection) { }

        /// <summary>
        /// Called on Update if NetworkApplication is client.
        /// </summary>
        protected internal virtual void ClientUpdate() { }

        /// <summary>
        /// Called on FixedUpdate if NetworkApplication is client.
        /// </summary>
        protected internal virtual void ClientFixedUpdate() { }

        /// <summary>
        /// Called on Update if NetworkApplication is server.
        /// </summary>
        protected internal virtual void ServerUpdate() { }

        /// <summary>
        /// Called on FixedUpdate if NetworkApplication is server.
        /// </summary>
        protected internal virtual void ServerFixedUpdate() { }
    }
}