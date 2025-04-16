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

        // TODO Don't use string. Optimize data size
        private readonly Dictionary<string, NetworkDataHandler> m_dataHandlers = new();

        public NetworkIdentity Identity { get; internal set; }
        public byte ComponentId { get; internal set; }

        internal static void DataHandler(NetworkConnection connection, NetworkReader reader)
        {
            NetworkBehaviour behaviour = reader.ReadNetworkBehaviour();
            string handlerName = reader.ReadString();

            if (behaviour.m_dataHandlers.TryGetValue(handlerName, out var handler) == false)
                throw new Exception($"Could not be found a data handler with name: {handlerName}");

            // TODO Use try catch
            handler(connection, reader);
        }

        protected void RegisterDataHandler(string handlerName, NetworkDataHandler handler)
        {
            ThrowHelper.ThrowIfNull(handler, nameof(handlerName));

            if (string.IsNullOrEmpty(handlerName))
                throw new ArgumentException(nameof(handlerName));

            if (m_dataHandlers.ContainsKey(handlerName))
            {
                ThrowHelper.RepeatedHandlerRegister(handlerName);
            }

            m_dataHandlers[handlerName] = handler;
        }

        protected void RegisterDataHandler(string handlerName, Action<NetworkReader> handler)
        {
            RegisterDataHandler(handlerName, (conn, reader) => handler(reader));
        }

        protected void RegisterMessageCallback<TMessage>(NetworkMessageCallback<TMessage> callback)
            where TMessage : unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            if (m_dataHandlers.ContainsKey(handlerName))
            {
                ThrowHelper.RepeatedMessageRegister(handlerName);
            }

            m_dataHandlers[handlerName] = MessageHandling.GetMessageHandler<TMessage>(callback);
        }

        protected void RegisterMessageCallback<TMessage>(Action<TMessage> callback)
            where TMessage: unmanaged, INetworkMessage
        {
            RegisterMessageCallback<TMessage>((conn, message) => callback(message));
        }

        protected bool RemoveDataHandler(string handlerName)
        {
            return m_dataHandlers.Remove(handlerName);
        }

        protected bool RemoveMessageCallback<TMessage>()
            where TMessage: unmanaged, INetworkMessage
        {
            string handlerName = MessageHandling.GetMessageHandlerName<TMessage>();

            return RemoveDataHandler(handlerName);
        }

        protected void SendData(NetworkConnection connection, string handlerName, ArraySegment<byte> data)
        {
            using NetworkWriterPooled writer = ProcessData(handlerName);
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

            using NetworkWriterPooled writer = ProcessData(handlerName);
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

        private NetworkWriterPooled ProcessData(string handlerName)
        {
            NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteNetworkBehaviour(this);
            writer.WriteString(handlerName);
            return writer;
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
    }
}