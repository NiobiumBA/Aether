using System;

namespace Aether
{
    /// <summary>
    /// Provide connection and disconnection,
    /// low-level send and receive bytes data to client/server.
    /// </summary>
    public abstract class NetworkTransport
    {
        public enum DisconnectType
        {
            // The connection is disconnected from its side.
            Forced,
            // The connection is disconnected from other side.
            Itself
        }


        public abstract class TransportError { }


        public delegate void ClientDataReceived(ArraySegment<byte> data);
        public delegate void ServerDataReceived(uint connectionId, ArraySegment<byte> data);
        public delegate void TransportErrorDelegate(TransportError error);


        public event TransportErrorDelegate OnTransportError;


        // --- Invoke on clients
        public event Action OnClientConnect;

        public event ClientDataReceived OnClientDataReceive;

        /// <summary>
        /// Invoked on client if this Transport is disconnected
        /// by calling the ClientDisconnect method of this instance.
        /// </summary>
        public event Action OnClientSelfDisconnect;

        /// <summary>
        /// Invoked on client if this Transport is disconnected
        /// by calling the ServerDisconnect method.
        /// </summary>
        public event Action OnClientForceDisconnect;
        // ---


        // --- Invoke on server
        public event Action<uint> OnServerConnect;

        public event ServerDataReceived OnServerDataReceive;

        /// <summary>
        /// Invoked on client if this Transport is disconnected
        /// by calling the ServerDisconnect method of this instance.
        /// </summary>
        public event Action<uint> OnServerSelfDisconnect;

        /// <summary>
        /// Invoked on client if this Transport is disconnected
        /// by calling the ClientDisconnect method.
        /// </summary>
        public event Action<uint> OnServerForceDisconnect;
        // ---

        /// <summary>
        /// This value is using in Batcher in NetworkConnection.
        /// </summary>
        public abstract int GetDataThreshold();

        public abstract void ClientConnect(string address);

        public abstract void SendToClient(uint connectionId, ArraySegment<byte> data);

        public abstract void SendToServer(ArraySegment<byte> data);

        public abstract void ClientDisconnect();

        public abstract void ServerDisconnect(uint connectionId);

        protected void OnTransportErrorEventInvoke(TransportError error)
        {
            OnTransportError?.Invoke(error);
        }

        protected void OnClientConnectEventInvoke()
        {
            OnClientConnect?.Invoke();
        }

        protected void OnClientDataReceiveEventInvoke(ArraySegment<byte> data)
        {
            OnClientDataReceive?.Invoke(data);
        }

        protected void OnClientDisconnectEventInvoke(DisconnectType type)
        {
            if (type == DisconnectType.Forced)
                OnClientForceDisconnect?.Invoke();
            else
                OnClientSelfDisconnect?.Invoke();
        }

        protected void OnServerConnectEventInvoke(uint connectionId)
        {
            OnServerConnect?.Invoke(connectionId);
        }

        protected void OnServerDataReceiveEventInvoke(uint connectionId, ArraySegment<byte> data)
        {
            OnServerDataReceive?.Invoke(connectionId, data);
        }

        protected void OnServerDisconnectEventInvoke(uint connectionId, DisconnectType type)
        {
            if (type == DisconnectType.Forced)
                OnServerForceDisconnect?.Invoke(connectionId);
            else
                OnServerSelfDisconnect?.Invoke(connectionId);
        }
    }
}
