using System;
using System.Collections.Generic;

namespace Aether.SceneManagement
{
    internal class CallbackQueue<TKey, TCallback>
    {
        private readonly Dictionary<TKey, Queue<TCallback>> m_callbacks = new();

        public void Enqueue(TKey key, TCallback callback)
        {
            if (m_callbacks.ContainsKey(key))
            {
                m_callbacks[key].Enqueue(callback);
                return;
            }

            Queue<TCallback> queue = new();
            queue.Enqueue(callback);
            m_callbacks.Add(key, queue);
            return;
        }

        public bool TryDequeue(TKey key, out TCallback callback)
        {
            if (m_callbacks.ContainsKey(key) == false)
            {
                callback = default;
                return false;
            }

            Queue<TCallback> queue = m_callbacks[key];

            callback = queue.Dequeue();

            if (queue.Count == 0)
                m_callbacks.Remove(key);

            return true;
        }

        public TCallback Dequeue(TKey key)
        {
            if (TryDequeue(key, out TCallback callback) == false)
                throw new ArgumentException(nameof(key));

            return callback;
        }
    }
}
