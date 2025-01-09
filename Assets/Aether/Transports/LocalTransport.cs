using System;
using System.Collections.Generic;

namespace Aether.Transports
{
    /// <summary>
    /// Provides connection the application to self.
    /// </summary>
    public class LocalTransport : NetworkTransport
    {
        public uint ConnectionId { get; private set; }

        private readonly Queue<ArraySegment<byte>> m_clientReceivingData = new();
        private readonly Queue<ArraySegment<byte>> m_serverReceivingData = new();

        private bool m_subscribedEventSystem = false;

        public LocalTransport(uint connectionId)
        {
            ConnectionId = connectionId;
        }

        public override int GetDataThreshold()
        {
            return int.MaxValue;
        }

        public override void ClientConnect(string address)
        {
            OnClientConnectEventInvoke();
            OnServerConnectEventInvoke(ConnectionId);

            SubscribeEventSystem();
        }

        public override void SendToClient(uint connectionId, ArraySegment<byte> data)
        {
            CheckConnectionId(connectionId);

            SendToClient(data);
        }

        public void SendToClient(ArraySegment<byte> data)
        {
            m_clientReceivingData.Enqueue(data);
        }

        public override void SendToServer(ArraySegment<byte> data)
        {
            m_serverReceivingData.Enqueue(data);
        }

        public override void ClientDisconnect()
        {
            OnClientDisconnectEventInvoke(DisconnectType.Itself);
            OnServerDisconnectEventInvoke(ConnectionId, DisconnectType.Forced);

            UnsubscribeEventSystem();
        }

        public override void ServerDisconnect(uint connectionId)
        {
            CheckConnectionId(connectionId);

            OnServerDisconnectEventInvoke(connectionId, DisconnectType.Itself);
            OnClientDisconnectEventInvoke(DisconnectType.Forced);

            UnsubscribeEventSystem();
        }

        private void BeforeHandleData()
        {
            while (m_clientReceivingData.TryDequeue(out ArraySegment<byte> data))
                OnClientDataReceiveEventInvoke(data);

            while (m_serverReceivingData.TryDequeue(out ArraySegment<byte> data))
                OnServerDataReceiveEventInvoke(ConnectionId, data);
        }

        private void CheckConnectionId(uint connectionId)
        {
            if (connectionId != ConnectionId)
                throw new ArgumentException($"{nameof(connectionId)} must be equals {nameof(ConnectionId)}({ConnectionId})",
                                            nameof(connectionId));
        }

        private void SubscribeEventSystem()
        {
            if (m_subscribedEventSystem)
                return;

            NetworkEventSystem.BeforeHandleData += BeforeHandleData;

            m_subscribedEventSystem = true;
        }

        private void UnsubscribeEventSystem()
        {
            if (m_subscribedEventSystem == false)
                return;

            NetworkEventSystem.BeforeHandleData -= BeforeHandleData;

            m_subscribedEventSystem = false;
        }
    }
}
