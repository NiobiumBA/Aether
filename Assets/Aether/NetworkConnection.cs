using System;
using System.Collections.Generic;

namespace Aether
{
    /// <summary>
    /// Encapsulate sending data divided by Batcher when NetworkEventSystem.SendData is invoked
    /// and handling receiving data divided by Unbatcher when NetworkEventSystem.HandleData is invoked.
    /// </summary>
    public abstract class NetworkConnection
    {
        public delegate void ConnectionAction(NetworkConnection connection);
        public delegate void ConnectionAction<T>(NetworkConnection connection, T args);

        /// <summary>
        /// Event HandleData is invoked in end of frame
        /// before sending all accumulated data.
        /// </summary>
        public event ConnectionAction<ArraySegment<byte>> HandleData;

        /// <summary>
        /// Called when the connection is disconnected from its side.
        /// </summary>
        public event ConnectionAction OnSelfDisconnect;

        /// <summary>
        /// Called when the connection is disconnected on the other side.
        /// </summary>
        public event ConnectionAction OnForcedDisconnect;

        private readonly NetworkTransport m_transport;

        private Batcher m_batcher;
        private Unbatcher m_unbatcher = new();
        private Queue<ArraySegment<byte>> m_dataQueue = new();
        private bool m_isActive = true;

        public NetworkTransport Transport => m_transport;

        public bool IsActive => m_isActive;

        public NetworkConnection(NetworkTransport transport)
        {
            m_batcher = new Batcher(transport.GetDataThreshold());
            m_transport = transport;

            NetworkEventSystem.SendData += SendAllBatches;
            NetworkEventSystem.HandleData += HandleAccumulatedData;
        }

        public void Send(ArraySegment<byte> data)
        {
            CheckIsActive();

            m_batcher.Enqueue(data);
        }

        public virtual void Disconnect()
        {
            if (m_isActive == false)
                return;

            SendAllBatches();

            BothDisconnect();
            OnSelfDisconnect?.Invoke(this);
        }

        protected virtual void ForcedDisconnect()
        {
            if (m_isActive == false)
                return;

            HandleAccumulatedData();

            BothDisconnect();
            OnForcedDisconnect?.Invoke(this);
        }

        protected void EnqueueReceivedData(ArraySegment<byte> data)
        {
            CheckIsActive();

            m_unbatcher.Enqueue(data);
        }

        /// <summary>
        /// SendToTransport is called when NetworkEventSystem.Send is invoking.
        /// This method send stored in batches data.
        /// </summary>
        /// <param name="data"></param>
        protected abstract void SendToTransport(ArraySegment<byte> data);

        private void SendAllBatches()
        {
            while (m_batcher.TryDequeue(out NetworkWriterPooled batch))
            {
                SendToTransport(batch.ToArraySegment());
                NetworkWriterPool.Return(batch);
            }
        }

        private void HandleAccumulatedData()
        {
            while (TryDequeAccumulatedData(out ArraySegment<byte> data))
            {
                HandleData?.Invoke(this, data);
            }
        }

        private bool TryDequeAccumulatedData(out ArraySegment<byte> data)
        {
            try
            {
                return m_unbatcher.TryDequeue(out data);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"Unable to dequeue a receiving data: {exception}");

                Disconnect();

                data = ArraySegment<byte>.Empty;
                return false;
            }
        }

        private void BothDisconnect()
        {
            NetworkEventSystem.SendData -= SendAllBatches;
            NetworkEventSystem.HandleData -= HandleAccumulatedData;

            m_isActive = false;

            m_batcher.Clear();
            m_unbatcher.Clear();
        }

        private void CheckIsActive()
        {
            if (m_isActive == false)
                throw new InvalidOperationException("This connection is not active");
        }
    }
}
