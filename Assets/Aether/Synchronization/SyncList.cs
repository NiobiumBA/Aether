using System;
using System.Collections;
using System.Collections.Generic;

namespace Aether.Synchronization
{
    public class SyncList<T> : SyncCollection<T>, IList<T>, IList, IReadOnlyList<T>
        where T : unmanaged
    {
        protected const short ReplaceOperation = 101;
        protected const short RemoveAtOperation = 102;
        protected const short InsertOperation = 103;
        protected const short MoveOperation = 104;

        private readonly List<T> m_list;

        public T this[int index]
        {
            get => m_list[index];
            set
            {
                m_list[index] = value;

                SendChanges((writer) => WriteReplaceOperationData(writer, index, value));
            }
        }

        protected override ICollection<T> Collection => m_list;

        bool IList.IsFixedSize => ((IList)m_list).IsFixedSize;

        object ICollection.SyncRoot => ((ICollection)m_list).SyncRoot;

        bool ICollection.IsSynchronized => ((ICollection)m_list).IsSynchronized;

        object IList.this[int index] { get => ((IList)m_list)[index]; set => ((IList)m_list)[index] = value; }

        public SyncList(NetworkBehaviour owner, SyncMode mode) : base(owner, mode)
        {
            RegisterAllHandlerChanges();

            m_list = new List<T>();
        }

        public SyncList(NetworkBehaviour owner, SyncMode mode, int capacity) : base(owner, mode)
        {
            RegisterAllHandlerChanges();

            m_list = new List<T>(capacity);
        }

        public int IndexOf(T item)
        {
            return m_list.IndexOf(item);
        }

        public void Move(int oldId, int newId)
        {
            MoveWithoutSendChanges(oldId, newId);

            SendChanges((writer) => WriteMoveOperationData(writer, oldId, newId));
        }

        public void Insert(int index, T item)
        {
            m_list.Insert(index, item);

            SendChanges((writer) => WriteInsertOperationData(writer, index, item));
        }

        public void RemoveAt(int index)
        {
            m_list.RemoveAt(index);

            SendChanges((writer) => WriteRemoveAtOperationData(writer, index));
        }

        private void RegisterAllHandlerChanges()
        {
            RegisterChangesHandler(ReplaceOperation, ApplyReplaceOperation);
            RegisterChangesHandler(RemoveAtOperation, ApplyRemoveAtOperation);
            RegisterChangesHandler(InsertOperation, ApplyInsertOperation);
            RegisterChangesHandler(MoveOperation, ApplyMoveOperation);
        }

        private void ApplyReplaceOperation(NetworkReader reader)
        {
            int id = reader.ReadInt();
            T replacedValue = reader.ReadBlittable<T>();

            m_list[id] = replacedValue;
        }

        private void ApplyRemoveAtOperation(NetworkReader reader)
        {
            int id = reader.ReadInt();

            m_list.RemoveAt(id);
        }

        private void ApplyInsertOperation(NetworkReader reader)
        {
            int id = reader.ReadInt();
            T item = reader.ReadBlittable<T>();

            m_list.Insert(id, item);
        }

        private void ApplyMoveOperation(NetworkReader reader)
        {
            int oldId = reader.ReadInt();
            int newId = reader.ReadInt();

            MoveWithoutSendChanges(oldId, newId);
        }

        private void WriteReplaceOperationData(NetworkWriter writer, int id, T value)
        {
            writer.WriteShort(ReplaceOperation);
            writer.WriteInt(id);
            writer.WriteBlittable(value);
        }

        private void WriteRemoveAtOperationData(NetworkWriter writer, int id)
        {
            writer.WriteShort(RemoveAtOperation);
            writer.WriteInt(id);
        }

        private void WriteInsertOperationData(NetworkWriter writer, int id, T item)
        {
            writer.WriteShort(InsertOperation);
            writer.WriteInt(id);
            writer.WriteBlittable(item);
        }

        private void WriteMoveOperationData(NetworkWriter writer, int oldId, int newId)
        {
            writer.WriteShort(MoveOperation);
            writer.WriteInt(oldId);
            writer.WriteInt(newId);
        }

        protected void MoveWithoutSendChanges(int oldId, int newId)
        {
            T value = m_list[oldId];
            m_list.RemoveAt(oldId);
            m_list.Insert(newId, value);
        }

        int IList.Add(object value)
        {
            return ((IList)m_list).Add(value);
        }

        bool IList.Contains(object value)
        {
            return ((IList)m_list).Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)m_list).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)m_list).Insert(index, value);
        }

        void IList.Remove(object value)
        {
            ((IList)m_list).Remove(value);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)m_list).CopyTo(array, index);
        }
    }
}
