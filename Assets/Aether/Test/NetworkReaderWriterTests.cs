using Aether;
using Aether.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aether.Test
{
    public class NetworkReaderWriterTests
    {
        public struct MyMessage : INetworkMessage
        {
            public int a;
            public BoolMessage b;
        }

        private static void WriteAndReadTest<T>(T expected, Action<NetworkWriter, T> writing, Func<NetworkReader, T> reading)
        {
            WriteAndReadTest<T>(expected, writing, reading, EqualityComparer<T>.Default);
        }

        private static void WriteAndReadTest<T>(T expected,
                                                Action<NetworkWriter, T> writing,
                                                Func<NetworkReader, T> reading,
                                                IEqualityComparer<T> comparer)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writing(writer, expected);

            ArraySegment<byte> data = writer.ToArraySegment();
            NetworkReader reader = new(data);
            T actual = reading(reader);

            Assert.IsTrue(comparer.Equals(expected, actual));
        }

        [Test]
        public void WriteAndRead10Int()
        {
            WriteAndReadTest<int>(10, (writer, value) => writer.WriteInt(value), (reader) => reader.ReadInt());
        }

        [Test]
        public void WriteAndRead10UInt()
        {
            WriteAndReadTest<uint>(10, (writer, value) => writer.WriteUInt(value), (reader) => reader.ReadUInt());
        }

        [Test]
        public void WriteAndReadTrueBool()
        {
            WriteAndReadTest<bool>(true, (writer, value) => writer.WriteBool(value), (reader) => reader.ReadBool());
        }

        [Test]
        public void WriteAndReadFalseBool()
        {
            WriteAndReadTest<bool>(false, (writer, value) => writer.WriteBool(value), (reader) => reader.ReadBool());
        }

        [Test]
        public void WriteAndReadMyStruct()
        {
            MyMessage structValue = new()
            {
                a = 10,
                b = false
            };
            WriteAndReadTest<MyMessage>(structValue, (writer, value) => writer.WriteMessage(value), (reader) => reader.ReadMessage<MyMessage>());
        }

        [Test]
        public void WriteAndReadString_Hello_world()
        {
            WriteAndReadTest<string>("Hello world!", (writer, value) => writer.WriteString(value), (reader) => reader.ReadString());
        }

        [Test]
        public void WriteAndReadStringNull()
        {
            WriteAndReadTest<string>(null, (writer, value) => writer.WriteString(value), (reader) => reader.ReadString());
        }

        [Test]
        public void WriteAndReadEmptyString()
        {
            WriteAndReadTest<string>(string.Empty, (writer, value) => writer.WriteString(value), (reader) => reader.ReadString());
        }

        [Test]
        public void ShortEnumerableEqualityComparerTestExpectedTrue()
        {
            short[] array1 = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            short[] array2 = new short[array1.Length];
            Array.Copy(array1, array2, array1.Length);
            EnumerableEqualityComparer<short> equalityComparer = new();

            bool expected = true;

            bool actual = equalityComparer.Equals(array1, array2);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void WriteAndReadShortArray()
        {
            short[] array = (new short[20]).Select((elem, id) => (short)id)
                                           .Reverse()
                                           .ToArray();

            EnumerableEqualityComparer<short> comparer = new();

            WriteAndReadTest<short[]>(array,
                             (writer, value) => writer.WriteBlittableArray(value),
                             (reader) => reader.ReadBlittableArray<short>(),
                             comparer);
        }

        [Test]
        public void WriteAndReadUShortArrayNull()
        {
            EnumerableEqualityComparer<ushort> comparer = new();

            WriteAndReadTest<ushort[]>(null,
                                       (writer, value) => writer.WriteBlittableArray(value),
                                       (reader) => reader.ReadBlittableArray<ushort>(),
                                       comparer);
        }

        [Test]
        public void WriteAndReadLongList()
        {
            List<long> list = (new long[100]).Select((elem, id) => (long)id)
                                             .ToList();

            EnumerableEqualityComparer<long> comparer = new();

            WriteAndReadTest<List<long>>(list,
                                         (writer, value) => writer.WriteBlittableList(value),
                                         (reader) => reader.ReadBlittableList<long>(),
                                         comparer);
        }

        [Test]
        public void WriteAndReadEmptyLongList()
        {
            List<long> list = new();

            EnumerableEqualityComparer<long> comparer = new();

            WriteAndReadTest<List<long>>(list,
                                         (writer, value) => writer.WriteBlittableList(value),
                                         (reader) => reader.ReadBlittableList<long>(),
                                         comparer);
        }

        [Test]
        public void WriteAndReadULongListNull()
        {
            EnumerableEqualityComparer<ulong> comparer = new();

            WriteAndReadTest<List<ulong>>(null,
                                         (writer, value) => writer.WriteBlittableList(value),
                                         (reader) => reader.ReadBlittableList<ulong>(),
                                         comparer);
        }

        [Test]
        public void WriteAndReadSByteList_String_Hello_Network()
        {
            EnumerableEqualityComparer<sbyte> comparer = new();
            List<sbyte> sbytesExpected = (new sbyte[167]).Select((elem, id) => (sbyte)(id / 2))
                                                         .ToList();
            string strExpected = "Hello Network!";

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteBlittableList<sbyte>(sbytesExpected);
            writer.WriteString(strExpected);

            NetworkReader reader = new(writer.ToArraySegment());

            List<sbyte> actualList = reader.ReadBlittableList<sbyte>();
            string actualString = reader.ReadString();

            Assert.IsTrue(comparer.Equals(sbytesExpected, actualList));
            Assert.AreEqual(strExpected, actualString);
        }

        [Test]
        public void WriteAndReadBytesWithLength()
        {
            EnumerableEqualityComparer<byte> comparer = new();
            ArraySegment<byte> bytesExpected = (new byte[167]).Select((elem, id) => (byte)(id / 2))
                                                  .ToArray();

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteBytesWithLength(bytesExpected);

            NetworkReader reader = new(writer.ToArraySegment());

            ArraySegment<byte> actualList = reader.ReadBytesWithLength();

            Assert.IsTrue(comparer.Equals(bytesExpected, actualList));
        }

        [Test]
        public void WriteAndReadBytes()
        {
            EnumerableEqualityComparer<byte> comparer = new();
            int count = 167;
            ArraySegment<byte> bytesExpected = (new byte[count]).Select((elem, id) => (byte)(id / 2))
                                                  .ToArray();

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteBytes(bytesExpected);

            NetworkReader reader = new(writer.ToArraySegment());

            ArraySegment<byte> actualList = reader.ReadBytes(count);

            Assert.IsTrue(comparer.Equals(bytesExpected, actualList));
        }

        [Test]
        public void WriteAndReadZeroBytes()
        {
            EnumerableEqualityComparer<byte> comparer = new();
            int count = 0;
            string text = "Hello Network";
            ArraySegment<byte> bytesExpected = ArraySegment<byte>.Empty;

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteString(text);
            writer.WriteBytes(bytesExpected);

            NetworkReader reader = new(writer.ToArraySegment());

            string actualText = reader.ReadString();
            ArraySegment<byte> actualBytes = reader.ReadBytes(count);

            Assert.AreEqual(text, actualText);
            Assert.IsTrue(comparer.Equals(bytesExpected, actualBytes));
        }
    }
}
