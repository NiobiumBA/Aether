﻿using System;
using System.Collections;
using System.Collections.Generic;
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

        public virtual void RemoveRange(IEnumerable<T> range)
        {
            RemoveRangeWithoutSend(range);

            SendChanges((writer) => WriteAddOperationData(writer, range));
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

        protected override ArraySegment<byte> GetInitData()
        {
            NetworkWriter writer = new();

            WriteInitializationOperation(writer);

            return writer.ToArraySegment();
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
            RemoveRangeWithoutSend(removedRange);
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

        protected void RemoveRangeWithoutSend(IEnumerable<T> range)
        {
            foreach (T item in range)
            {
                if (Collection.Remove(item) == false)
                    Debug.LogError($"Failed to remove {item}.");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.GetEnumerator();
        }
    }
}