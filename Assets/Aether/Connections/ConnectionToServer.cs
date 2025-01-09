using System;

namespace Aether.Connections
{
    public class ConnectionToServer : NetworkConnection
    {
        public ConnectionToServer(NetworkTransport transport) : base(transport)
        {
            SubscribeToTransportEvents();
        }

        public override void Disconnect()
        {
            OnClientSelfDisconnect();

            Transport.ClientDisconnect();
        }

        protected override void ForcedDisconnect()
        {
            base.ForcedDisconnect();

            UnsubscribeTransportEvents();
        }

        protected override void SendToTransport(ArraySegment<byte> data)
        {
            Transport.SendToServer(data);
        }

        private void OnClientSelfDisconnect()
        {
            base.Disconnect();

            UnsubscribeTransportEvents();
        }

        private void SubscribeToTransportEvents()
        {
            Transport.OnClientDataReceive += EnqueueReceivedData;
            Transport.OnClientSelfDisconnect += OnClientSelfDisconnect;
            Transport.OnClientForceDisconnect += ForcedDisconnect;
        }

        private void UnsubscribeTransportEvents()
        {
            Transport.OnClientDataReceive -= EnqueueReceivedData;
            Transport.OnClientSelfDisconnect -= OnClientSelfDisconnect;
            Transport.OnClientForceDisconnect -= ForcedDisconnect;
        }
    }
}
