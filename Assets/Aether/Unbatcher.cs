using System;

namespace Aether
{
    /// <summary>
    /// One ArraySegment can contains more than one batch.
    /// Requires that single batch stored in one or two which added one after the other receiving ArraySegments.
    /// </summary>
    public class Unbatcher
    {
        private NetworkWriter m_writer = new();
        private NetworkWriter m_secondWriter = new();
        private bool m_isReadPacketInfoNext = true;
        private int m_packetSize = 0;

        public void Enqueue(ArraySegment<byte> data)
        {
            m_writer.WriteBytes(data);
        }

        public bool TryDequeue(out ArraySegment<byte> data)
        {
            if (m_writer.Position == 0)
            {
                data = ArraySegment<byte>.Empty;
                return false;
            }

            NetworkReader reader = new(m_writer.ToArraySegment());

            bool isUpdateWriter = false;

            if (m_isReadPacketInfoNext)
            {
                if (reader.Remaining < Batcher.PacketInfoSize)
                {
                    data = ArraySegment<byte>.Empty;
                    return false;
                }

                isUpdateWriter = true;
                m_packetSize = reader.ReadInt();

                m_isReadPacketInfoNext = false;
            }

            if (m_packetSize <= reader.Remaining)
            {
                data = reader.ReadBytes(m_packetSize);

                m_isReadPacketInfoNext = true;
                m_packetSize = 0;
                UpdateWriter(reader);

                return true;
            }

            if (isUpdateWriter)
                UpdateWriter(reader);

            data = ArraySegment<byte>.Empty;
            return false;
        }

        public void Clear()
        {
            m_writer.Clear();
            m_secondWriter.Clear();
            m_isReadPacketInfoNext = true;
            m_packetSize = 0;
        }

        private void UpdateWriter(NetworkReader reader)
        {
            m_writer.Clear();
            (m_writer, m_secondWriter) = (m_secondWriter, m_writer);
            m_writer.WriteBytes(reader.ToArraySegment());
        }
    }
}
