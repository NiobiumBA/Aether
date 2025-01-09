using Aether.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aether
{
    public static class NetworkWriterExtensions
    {
        internal const int c_sizeEnumerableIfNull = -1;

        public static readonly Encoding StringEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static void WriteBool(this NetworkWriter writer, bool value)
        {
            writer.WriteBlittable(value ? 1 : 0);
        }

        public static void WriteByte(this NetworkWriter writer, byte value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteSByte(this NetworkWriter writer, sbyte value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteShort(this NetworkWriter writer, short value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteUShort(this NetworkWriter writer, ushort value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteInt(this NetworkWriter writer, int value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteUInt(this NetworkWriter writer, uint value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteLong(this NetworkWriter writer, long value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteULong(this NetworkWriter writer, ulong value)
        {
            writer.WriteBlittable(value);
        }

        public static void WriteBytesWithLength(this NetworkWriter writer, ArraySegment<byte> bytes)
        {
            writer.WriteInt(bytes.Count);
            writer.WriteBytes(bytes);
        }

        public static void WriteBlittableArray<T>(this NetworkWriter writer, T[] values)
            where T : unmanaged
        {
            if (values == null)
            {
                writer.WriteInt(c_sizeEnumerableIfNull);
                return;
            }

            writer.WriteBlittableArraySegment(new ArraySegment<T>(values));
        }

        public static void WriteBlittableArraySegment<T>(this NetworkWriter writer, ArraySegment<T> segment)
            where T : unmanaged
        {
            writer.WriteInt(segment.Count);

            for (int i = segment.Offset; i < segment.Offset + segment.Count; i++)
            {
                writer.WriteBlittable(segment.Array[i]);
            }
        }

        public static void WriteBlittableList<T>(this NetworkWriter writer, List<T> list)
            where T : unmanaged
        {
            if (list == null)
            {
                writer.WriteInt(c_sizeEnumerableIfNull);
                return;
            }

            writer.WriteInt(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                writer.WriteBlittable(list[i]);
            }
        }

        public static void WriteMessage<TMessage>(this NetworkWriter writer, TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            writer.WriteBlittable(message);
        }

        public static void WriteString(this NetworkWriter writer, string text)
        {
            if (text == null)
            {
                writer.WriteInt(c_sizeEnumerableIfNull);
                return;
            }

            byte[] bytes = StringEncoding.GetBytes(text);

            writer.WriteInt(bytes.Length);
            writer.WriteBytes(bytes);
        }
    }
}
