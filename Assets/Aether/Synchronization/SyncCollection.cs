using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aether.Synchronization
{
    public abstract class SyncCollection<T> : SyncObject, ICollection<T>, IReadOnlyCollection<T>
        where T : unmanaged
    {
        protected const short InitializationOperation = 0;
        protected const short AddRangeOperation = 1;
        protected const short RemoveRangeOperation = 2;

        protected readonly Dictionary<short, Action<NetworkReader>> changeHandlers = new();

        public int Count => Collection.Count;
        public bool IsReadOnly => Collection.IsReadOnly;

        protected abstract ICollection<T> Collection { get; }

        static SyncCollection()
        {
            if (NetworkWriter.IsSerializable<T>() == false)
                ThrowHelper.ArgumentNonSerializableType(typeof(T));
        }

        protected SyncCollection(NetworkBehaviour owner, SyncMode mode) : base(owner, mode)
        {
            RegisterChangesHandler(InitializationOperation, ApplyInitializationOperation);
            RegisterChangesHandler(AddRangeOperation, ApplyAddOperation);
            RegisterChangesHandler(RemoveRangeOperation, ApplyRemoveOperation);
        }

        public virtual void AddRange(IEnumerable<T> range)
        {
            AddRangeWithoutSend(range);

            SendChanges((writer) => WriteAddOperationData(writer, range));
        }

        /// <summary>
        /// </summary>
        /// <returns>Return true if all elements in range have been deleted, else false</returns>
        public virtual bool RemoveRange(IEnumerable<T> range)
        {
            bool result = true;

            bool[] successfulRemoving = new bool[range.Count()];

            int i = 0;
            foreach (T item in range)
            {
                bool currentSuccessful = Collection.Remove(item);
                successfulRemoving[i] = currentSuccessful;

                if (currentSuccessful == false)
                    result = false;

                i++;
            }

            IEnumerable<T> successRemoved = range
                .Zip(successfulRemoving, (T item, bool successful) => (item, successful))
                .Where(tuple => tuple.successful)
                .Select(tuple => tuple.item);

            SendChanges((writer) => WriteRemoveOperationData(writer, range));

            return result;
        }

        public virtual void Add(T item)
        {
            Collection.Add(item);

            SendChanges((writer) =>
            {
                T[] addedRange = new T[] { item };
                WriteAddOperationData(writer, addedRange);
            });
        }

        public virtual void Clear()
        {
            T[] array = new T[Collection.Count];
            Collection.CopyTo(array, 0);

            Collection.Clear();

            SendChanges((writer) => WriteRemoveOperationData(writer, array));
        }

        public virtual bool Contains(T item)
        {
            return Collection.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            Collection.CopyTo(array, arrayIndex);
        }

        public virtual bool Remove(T item)
        {
            if (Collection.Remove(item) == false)
                return false;

            SendChanges((writer) => WriteRemoveOperationData(writer, new T[] { item }));
            return true;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        protected override NetworkWriterPooled GetInitData()
        {
            NetworkWriterPooled writer = NetworkWriterPool.Get();
            WriteInitializationOperation(writer);
            return writer;
        }

        protected override void OnChangeReceived(NetworkReader reader, NetworkConnection connection)
        {
            short operation = reader.ReadShort();

            Action<NetworkReader> handler = changeHandlers[operation];

            handler(reader);
        }

        protected void RegisterChangesHandler(short handlerId, Action<NetworkReader> handler)
        {
            if (changeHandlers.ContainsKey(handlerId))
            {
                ThrowHelper.RepeatedHandlerRegister(handlerId.ToString());
            }

            changeHandlers[handlerId] = handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SendChanges(Action<NetworkWriter> writing)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writing(writer);

            SendChanges(writer.ToArraySegment());
        }

        protected virtual void ApplyInitializationOperation(NetworkReader reader)
        {
            List<T> values = reader.ReadBlittableList<T>();

            Collection.Clear();
            AddRangeWithoutSend(values);
        }

        protected virtual void ApplyAddOperation(NetworkReader reader)
        {
            List<T> addedRange = reader.ReadBlittableList<T>();
            AddRangeWithoutSend(addedRange);
        }

        protected virtual void ApplyRemoveOperation(NetworkReader reader)
        {
            List<T> removedRange = reader.ReadBlittableList<T>();
            
            foreach (T item in removedRange)
            {
                Collection.Remove(item);
            }
        }

        protected virtual void WriteInitializationOperation(NetworkWriter writer)
        {
            if (Collection is not List<T> list)
                list = new List<T>(Collection);

            writer.WriteShort(InitializationOperation);
            writer.WriteBlittableList(list);
        }

        protected virtual void WriteAddOperationData(NetworkWriter writer, IEnumerable<T> values)
        {
            if (Collection is not List<T> list)
                list = new List<T>(values);

            writer.WriteShort(AddRangeOperation);
            writer.WriteBlittableList(list);
        }

        protected virtual void WriteRemoveOperationData(NetworkWriter writer, IEnumerable<T> values)
        {
            if (Collection is not List<T> list)
                list = new List<T>(values);

            writer.WriteShort(RemoveRangeOperation);
            writer.WriteBlittableList(list);
        }

        protected void AddRangeWithoutSend(IEnumerable<T> range)
        {
            if (Collection is List<T> list)
            {
                list.AddRange(range);
                return;
            }

            foreach (T item in range)
            {
                Collection.Add(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
    }
}
