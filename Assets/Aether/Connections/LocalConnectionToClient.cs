using Aether.Transports;

namespace Aether.Connections
{
    public class LocalConnectionToClient : ConnectionToClient
    {
        public LocalConnectionToClient(LocalTransport transport)
            : base(transport, transport.ConnectionId)
        {
        }
    }
}
