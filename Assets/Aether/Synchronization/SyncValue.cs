﻿using System;

namespace Aether.Synchronization
{
    public class SyncValue<T> : SyncObject
        where T : unmanaged
    {
        public delegate T SetterConnectionDelegate(T value, NetworkConnection connection);
        public delegate T SetterDelegate(T value);

        public static readonly SetterConnectionDelegate DefaultSetter = (value, conn) => value;

        private readonly SetterConnectionDelegate m_setter;

        private T m_value;

        public T Value
        {
            get => m_value;
            set
            {
                NetworkConnection connection = NetworkApplication.ClientConnected ? NetworkApplication.ClientDispatcher.Connection : null;

                m_value = m_setter(value, connection);

                using NetworkWriterPooled writer = NetworkWriterPool.Get();
                writer.WriteBlittable(m_value);
                SendChanges(writer.ToArraySegment());
            }
        }

        static SyncValue()
        {
            if (NetworkWriter.IsSerializable<T>() == false)
                ThrowHelper.ArgumentNonSerializableType(typeof(T));
        }

        public SyncValue(NetworkBehaviour owner, SyncMode mode, T value) : base(owner, mode)
        {
            m_setter = DefaultSetter;
            m_value = value;
        }

        public SyncValue(NetworkBehaviour owner, SyncMode mode, T value, SetterConnectionDelegate setter) : base(owner, mode)
        {
            ThrowHelper.ThrowIfNull(setter, nameof(setter));

            m_setter = setter;
            m_value = value;
        }

        public SyncValue(NetworkBehaviour owner, SyncMode mode, T value, SetterDelegate setter) : base(owner, mode)
        {
            ThrowHelper.ThrowIfNull(setter, nameof(setter));

            m_setter = (value, connection) => setter(value);
            m_value = value;
        }

        protected override void OnChangeReceived(NetworkReader reader, NetworkConnection connection)
        {
            T receivedValue = reader.ReadBlittable<T>();
            m_value = m_setter(receivedValue, connection);
        }

        protected override void WriteInitData(NetworkWriter writer)
        {
            writer.WriteBlittable(m_value);
        }

        public static implicit operator T(SyncValue<T> value)
        {
            return value.m_value;
        }
    }
}
