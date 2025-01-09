using System;
using System.Collections.Generic;
using System.Linq;

namespace Aether.Connections
{
    /// <summary>
    /// Represents a connection with unique connectionId for a single Transport.
    /// </summary>
    public abstract class IdentifiableConnection : NetworkConnection
    {
        public readonly struct ConnectionIdentity : IEquatable<ConnectionIdentity>
        {
            public readonly NetworkTransport transport;
            public readonly uint connectionId;

            public ConnectionIdentity(NetworkTransport transport, uint connectionId)
            {
                this.transport = transport;
                this.connectionId = connectionId;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is ConnectionIdentity other && this == other;
            }

            public readonly bool Equals(ConnectionIdentity other)
            {
                return this == other;
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(transport, connectionId);
            }

            public static bool operator ==(ConnectionIdentity a, ConnectionIdentity b)
            {
                return a.transport == b.transport && a.connectionId == b.connectionId;
            }

            public static bool operator !=(ConnectionIdentity a, ConnectionIdentity b)
            {
                return !(a == b);
            }
        }

        protected abstract class EventHandler
        {
            public NetworkTransport Transport { get; }

            protected EventHandler(NetworkTransport transport)
            {
                Transport = transport;
            }

            public abstract void SubscribeEvents();
            public abstract void UnsubscribeEvents();
        }

        public static IReadOnlyDictionary<ConnectionIdentity, IdentifiableConnection> Connections => s_connections;

        private static readonly Dictionary<ConnectionIdentity, IdentifiableConnection> s_connections = new();
        private static readonly Dictionary<NetworkTransport, EventHandler> s_eventHandlers = new();

        private readonly uint m_connectionId;

        public uint ConnectionId => m_connectionId;

        protected IdentifiableConnection(NetworkTransport transport, uint connectionId) : base(transport)
        {
            m_connectionId = connectionId;

            ConnectionIdentity identity = new(transport, connectionId);

            if (s_connections.ContainsKey(identity))
                throw new InvalidOperationException($"A connection with same transport and connectionId has been created and is working");

            if (ContainsTransport(transport) == false)
                SubscribeTransportEvents();

            s_connections.Add(identity, this);
        }

        public override void Disconnect()
        {
            base.Disconnect();
            BothDisconnect();
        }

        protected abstract EventHandler GetEventHandler();

        protected override void ForcedDisconnect()
        {
            base.ForcedDisconnect();
            BothDisconnect();
        }

        private void BothDisconnect()
        {
            s_connections.Remove(new ConnectionIdentity(Transport, ConnectionId));

            if (ContainsTransport(Transport) == false)
                UnsubscribeTransportEvents();
        }

        private void SubscribeTransportEvents()
        {
            EventHandler eventHandler = GetEventHandler();
            eventHandler.SubscribeEvents();

            s_eventHandlers.Add(Transport, eventHandler);
        }

        private void UnsubscribeTransportEvents()
        {
            s_eventHandlers.Remove(Transport, out EventHandler eventHandler);
            eventHandler.UnsubscribeEvents();
        }

        private bool ContainsTransport(NetworkTransport transport)
        {
            return s_connections.Keys.Any(identity => identity.transport == transport);
        }
    }
}
