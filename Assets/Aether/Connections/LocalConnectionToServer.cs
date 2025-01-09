using Aether.Transports;

namespace Aether.Connections
{
    public class LocalConnectionToServer : ConnectionToServer
    {
        public LocalConnectionToServer(LocalTransport transport)
            : base(transport)
        {
        }
    }
}
