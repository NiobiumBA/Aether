using System;

namespace Aether.Connections
{
    public class ConnectionToClient : IdentifiableConnection
    {
        protected class ConnectionToClientEventHandler : EventHandler
        {
            public ConnectionToClientEventHandler(NetworkTransport transport) : base(transport)
            {
            }

            public override void SubscribeEvents()
            {
                Transport.OnServerDataReceive += OnServerDataReceive;
                Transport.OnServerSelfDisconnect += OnServerSelfDisconnect;
                Transport.OnServerForceDisconnect += OnServerForceDisconnect;
            }

            public override void UnsubscribeEvents()
            {
                Transport.OnServerDataReceive -= OnServerDataReceive;
                Transport.OnServerSelfDisconnect -= OnServerSelfDisconnect;
                Transport.OnServerForceDisconnect -= OnServerForceDisconnect;
            }

            private void OnServerDataReceive(uint connectionId, ArraySegment<byte> data)
            {
                IdentifiableConnection connection = Connections[new ConnectionIdentity(Transport, connectionId)];
                (connection as ConnectionToClient).EnqueueReceivedData(data);
            }

            private void OnServerSelfDisconnect(uint connectionId)
            {
                IdentifiableConnection connection = Connections[new ConnectionIdentity(Transport, connectionId)];
                (connection as ConnectionToClient).OnServerSelfDisconnect();
            }

            private void OnServerForceDisconnect(uint connectionId)
            {
                IdentifiableConnection connection = Connections[new ConnectionIdentity(Transport, connectionId)];
                (connection as ConnectionToClient).ForcedDisconnect();
            }
        }

        public ConnectionToClient(NetworkTransport transport, uint connectionId) : base(transport, connectionId)
        {
        }

        // TODO Fix double Disconnection
        public override void Disconnect()
        {
            OnServerSelfDisconnect();

            Transport.ServerDisconnect(ConnectionId);
        }

        protected override void SendToTransport(ArraySegment<byte> data)
        {
            Transport.SendToClient(ConnectionId, data);
        }

        protected override EventHandler GetEventHandler()
        {
            return new ConnectionToClientEventHandler(Transport);
        }

        private void OnServerSelfDisconnect()
        {
            base.Disconnect();
        }
    }
}
