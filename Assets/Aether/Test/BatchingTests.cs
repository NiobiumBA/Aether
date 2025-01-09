using Aether.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Aether.Test
{
    public class BatchingTests
    {
        private struct MyMessage : INetworkMessage
        {
            public GameObjectMessage originalGameObjectMessage;
            public uint netId;
            public Vector3 position;
            public Quaternion rotation;
            public GameObjectMessage parentGameObjectMessage;
        }

        [Test]
        public void BatchAndUnbatchNonPartialPackets()
        {
            EnumerableEqualityComparer<byte> packetComparer = new();
            EnumerableEqualityComparer<IEnumerable<byte>> packetsComparer = new(packetComparer);

            Batcher batcher = new(1024);

            string text = "Hello Network!";
            ArraySegment<byte> data = new(Encoding.UTF8.GetBytes(text));

            ArraySegment<byte>[] packets = new ArraySegment<byte>[]
            {
                data, data, data
            };


            foreach (var packet in packets)
            {
                batcher.Enqueue(packet);
            }

            Unbatcher unbatcher = new();

            while (batcher.TryDequeue(out NetworkWriterPooled batch))
            {
                unbatcher.Enqueue(batch.ToArraySegment());
            }

            CancellationTokenSource tokenSource = new();
            Task<List<ArraySegment<byte>>> getPacketsTask = Task.Run(() => GetAllPackets(unbatcher, tokenSource.Token));
            Task delayTask = Task.Delay(3 * 1000);

            Task<Task> whenAnyTask = Task.WhenAny(getPacketsTask, delayTask);
            whenAnyTask.Wait();
            Task firstCompletedTask = whenAnyTask.Result;

            if (firstCompletedTask == delayTask)
            {
                tokenSource.Cancel();

                Assert.Fail("Timed out");
                return;
            }

            List<ArraySegment<byte>> resultPackets = getPacketsTask.Result;

            Assert.IsTrue(packetsComparer.Equals(packets.Cast<IEnumerable<byte>>(), resultPackets.Cast<IEnumerable<byte>>()));
        }

        [Test]
        public void UnbatchPartialPackets()
        {
            EnumerableEqualityComparer<byte> packetComparer = new();
            EnumerableEqualityComparer<IEnumerable<byte>> packetsComparer = new(packetComparer);

            Batcher batcher = new(100);

            string[] texts = new string[]
            {
                "Hello Network!",
                "Hi",
                "Hello world"
            };

            List<ArraySegment<byte>> encodedText = new();

            foreach (string text in texts)
            {
                ArraySegment<byte> data = new(Encoding.UTF8.GetBytes(text));
                encodedText.Add(data);
                batcher.Enqueue(data);
            }

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            while (batcher.TryDequeue(out NetworkWriterPooled batch))
            {
                writer.WriteBytes(batch.ToArraySegment());
            }

            ArraySegment<byte> allDataInOne = writer.ToArraySegment();

            NetworkReader reader = new(allDataInOne);

            List<ArraySegment<byte>> dataParts = new();

            int partSize = 100;

            while (reader.Remaining > 0)
            {
                ArraySegment<byte> dataPart = reader.ReadBytes(Math.Min(reader.Remaining, partSize));
                dataParts.Add(dataPart);
            }

            Unbatcher unbatcher = new();

            foreach (var part in dataParts)
            {
                unbatcher.Enqueue(part);
            }

            List<ArraySegment<byte>> resultPackets = new();

            while (unbatcher.TryDequeue(out ArraySegment<byte> resultPacket))
            {
                resultPackets.Add(resultPacket);
            }

            Assert.IsTrue(packetsComparer.Equals(encodedText.Cast<IEnumerable<byte>>(), resultPackets.Cast<IEnumerable<byte>>()));
        }

        [Test]
        public void BatchAndUnbatchString()
        {
            string text = "Hello Network!";

            Batcher batcher = new(100);

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteString(text);

            batcher.Enqueue(writer.ToArraySegment());

            Unbatcher unbatcher = new();

            while (batcher.TryDequeue(out NetworkWriterPooled batch))
                unbatcher.Enqueue(batch.ToArraySegment());

            if (unbatcher.TryDequeue(out ArraySegment<byte> data) == false)
                Assert.Fail();

            if (unbatcher.TryDequeue(out _))
                Assert.Fail();

            NetworkReader reader = new(data);
            string resultText = reader.ReadString();

            Assert.AreEqual(text, resultText);
        }

        [Test]
        public void BatchAndUnbatchMessage()
        {
            Batcher batcher = new(1024);

            string handlerName = MessageHandling.GetMessageHandlerName<MyMessage>();
            MyMessage message = new();

            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteString(handlerName);
            writer.WriteMessage(message);

            batcher.Enqueue(writer.ToArraySegment());

            Unbatcher unbatcher = new();

            while (batcher.TryDequeue(out NetworkWriterPooled batch))
                unbatcher.Enqueue(batch.ToArraySegment());

            if (unbatcher.TryDequeue(out ArraySegment<byte> data) == false)
                Assert.Fail();

            NetworkReader reader = new(data);
            string actualHandlerName = reader.ReadString();
            MyMessage actualMessage = reader.ReadMessage<MyMessage>();

            Assert.AreEqual(handlerName, actualHandlerName);
            Assert.AreEqual(message, actualMessage);

            if (unbatcher.TryDequeue(out _))
                Assert.Fail();
        }

        private List<ArraySegment<byte>> GetAllPackets(Unbatcher unbatcher, CancellationToken token)
        {
            List<ArraySegment<byte>> result = new();

            while (unbatcher.TryDequeue(out ArraySegment<byte> data))
            {
                token.ThrowIfCancellationRequested();

                result.Add(data);
            }

            return result;
        }
    }
}