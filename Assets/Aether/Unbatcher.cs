using System;
using System.Collections.Generic;

namespace Aether
{
    /// <summary>
    /// One ArraySegment can contains more than one batch.
    /// Requires that single batch stored in one or two which added one after the other receiving ArraySegments.
    /// </summary>
    public class Unbatcher
    {
        private readonly Queue<ArraySegment<byte>> m_queue = new();
        private readonly NetworkWriter m_writer = new();
        private int m_offset = 0;
        private bool m_readPacketSize = true;
        private int m_packetSize;

        public void Enqueue(ArraySegment<byte> data)
        {
            m_queue.Enqueue(data);
        }

        public bool TryDequeue(out ArraySegment<byte> data)
        {
            if (m_queue.Count == 0)
            {
                data = ArraySegment<byte>.Empty;
                return false;
            }

            NetworkReader firstReader = new(m_queue.Peek());

            firstReader.ReadBytes(m_offset); // Skip bytes

            if (m_readPacketSize)
            {
                m_packetSize = firstReader.ReadInt();
                m_offset += Batcher.PacketInfoSize;
                m_readPacketSize = false;
            }

            if (m_packetSize <= firstReader.Remaining)
            {
                if (m_packetSize == firstReader.Remaining)
                {
                    m_offset = 0;
                    m_queue.Dequeue();
                }
                else
                {
                    m_offset += m_packetSize;
                }

                m_readPacketSize = true;

                data = firstReader.ReadBytes(m_packetSize);
                return true;
            }

            return TryDequeuePartialPacket(out data, firstReader);
        }

        public void Clear()
        {
            m_queue.Clear();
            m_offset = 0;
            m_readPacketSize = true;
        }

        private bool TryDequeuePartialPacket(out ArraySegment<byte> data, NetworkReader firstReader)
        {
            if (m_queue.Count == 1)
            {
                m_readPacketSize = false;
                data = ArraySegment<byte>.Empty;
                return false;
            }

            int firstPartSize = firstReader.Remaining;
            ArraySegment<byte> firstPacketPart = firstReader.ReadBytes(firstPartSize);

            m_queue.Dequeue();

            NetworkReader secondReader = new(m_queue.Peek());

            int secondPartSize = m_packetSize - firstPartSize;
            ArraySegment<byte> secondPacketPart = secondReader.ReadBytes(secondPartSize);

            m_offset = secondPartSize;
            m_readPacketSize = true;

            m_writer.Clear();
            m_writer.WriteBytes(firstPacketPart);
            m_writer.WriteBytes(secondPacketPart);

            data = m_writer.ToArraySegment();
            return true;
        }
    }
}
