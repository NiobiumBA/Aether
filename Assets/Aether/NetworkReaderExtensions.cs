using Aether.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aether
{
    public static class NetworkReaderExtensions
    {
        public static bool ReadBool(this NetworkReader reader)
        {
            return reader.ReadBlittable<byte>() != 0;
        }

        public static byte ReadByte(this NetworkReader reader)
        {
            return reader.ReadBlittable<byte>();
        }

        public static sbyte ReadSByte(this NetworkReader reader)
        {
            return reader.ReadBlittable<sbyte>();
        }

        public static short ReadShort(this NetworkReader reader)
        {
            return reader.ReadBlittable<short>();
        }

        public static ushort ReadUShort(this NetworkReader reader)
        {
            return reader.ReadBlittable<ushort>();
        }

        public static int ReadInt(this NetworkReader reader)
        {
            return reader.ReadBlittable<int>();
        }

        public static uint ReadUInt(this NetworkReader reader)
        {
            return reader.ReadBlittable<uint>();
        }

        public static long ReadLong(this NetworkReader reader)
        {
            return reader.ReadBlittable<long>();
        }

        public static ulong ReadULong(this NetworkReader reader)
        {
            return reader.ReadBlittable<ulong>();
        }

        //public static float ReadFloat(this NetworkReader reader)

        public static ArraySegment<byte> ReadBytesWithLength(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            return reader.ReadBytes(length);
        }

        public static T[] ReadBlittableArray<T>(this NetworkReader reader)
            where T : unmanaged
        {
            int count = reader.ReadInt();

            if (count == NetworkWriterExtensions.c_sizeEnumerableIfNull)
                return null;

            T[] array = new T[count];

            for (int i = 0; i < count; i++)
            {
                array[i] = reader.ReadBlittable<T>();
            }

            return array;
        }

        public static List<T> ReadBlittableList<T>(this NetworkReader reader)
            where T : unmanaged
        {
            T[] array = reader.ReadBlittableArray<T>();

            if (array == null)
                return null;

            return new List<T>(array);
        }

        public static TMessage ReadMessage<TMessage>(this NetworkReader reader)
            where TMessage : unmanaged, INetworkMessage
        {
            return reader.ReadBlittable<TMessage>();
        }

        public static string ReadString(this NetworkReader reader, Encoding encoding)
        {
            int length = reader.ReadInt();

            if (length == NetworkWriterExtensions.c_sizeEnumerableIfNull)
                return null;

            ArraySegment<byte> bytes = reader.ReadBytes(length);

            return encoding.GetString(bytes);
        }

        public static string ReadString(this NetworkReader reader)
        {
            return ReadString(reader, NetworkWriterExtensions.StringEncoding);
        }
    }
}
