using System;

namespace Aether
{
    public class NetworkWriterPooled : NetworkWriter, IDisposable
    {
        public void Dispose()
        {
            NetworkWriterPool.Return(this);
        }
    }
}
