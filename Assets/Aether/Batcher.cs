using System;
using System.Collections.Generic;

namespace Aether
{
    public class Batcher
    {
        public const int PacketInfoSize = sizeof(int);

        private readonly int m_threshold;

        private Queue<NetworkWriterPooled> m_batches = new();
        private NetworkWriterPooled m_lastBatch;

        public int Threshold => m_threshold;

        public Batcher(int threshold)
        {
            m_threshold = threshold;
        }

        public void Enqueue(ArraySegment<byte> data)
        {
            int countWithInfo = data.Count + PacketInfoSize;

            if (countWithInfo > m_threshold)
                throw new ArgumentException("The data is too big", nameof(data));

            if (m_lastBatch == null || m_lastBatch.Position + countWithInfo > m_threshold)
            {
                m_lastBatch = NetworkWriterPool.Get();
                m_batches.Enqueue(m_lastBatch);
            }

            m_lastBatch.WriteInt(data.Count);
            m_lastBatch.WriteBytes(data);
        }

        public bool TryDequeue(out NetworkWriterPooled batch)
        {
            if (m_batches.Count == 0)
            {
                batch = null;
                return false;
            }

            if (m_batches.Count == 1)
            {
                batch = m_batches.Dequeue();
                m_lastBatch = null;
                return true;
            }

            batch = m_batches.Dequeue();
            return true;
        }

        public void Clear()
        {
            while (m_batches.TryDequeue(out NetworkWriterPooled writer))
                NetworkWriterPool.Return(writer);

            m_lastBatch = null;
        }
    }
}
