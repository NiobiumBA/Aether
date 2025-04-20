using System;
using System.Collections.Generic;

namespace Aether
{
    public static class NetworkWriterPool
    {
        private static readonly Stack<NetworkWriterPooled> m_writers = new();

        public static NetworkWriterPooled Get()
        {
            if (m_writers.Count == 0)
                return new NetworkWriterPooled();

            return m_writers.Pop();
        }

        public static void Return(NetworkWriterPooled writer)
        {
            if (m_writers.Contains(writer))
                throw new ArgumentException("The writer has been already returned");

            writer.Clear();
            m_writers.Push(writer);
        }
    }
}
