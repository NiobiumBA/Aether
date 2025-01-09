using Aether;
using Aether.Connections;
using NUnit.Framework;
using System;

namespace Aether.Test
{
    public class ConnectionToClientTests
    {
        private class ServerTransport : NetworkTransport
        {
            public override int GetDataThreshold()
            {
                return 1024;
            }

            public void ServerForceDisconnect(uint connectionId)
            {
                OnServerDisconnectEventInvoke(connectionId, DisconnectType.Forced);
            }

            public override void ServerDisconnect(uint connectionId)
            {
                OnServerDisconnectEventInvoke(connectionId, DisconnectType.Itself);
            }

            public override void ClientConnect(string address)
            {
                throw new InvalidOperationException();
            }

            public override void ClientDisconnect()
            {
                throw new InvalidOperationException();
            }

            public override void SendToClient(uint connectionId, ArraySegment<byte> data)
            {
                throw new InvalidOperationException();
            }

            public override void SendToServer(ArraySegment<byte> data)
            {
                throw new InvalidOperationException();
            }
        }

        [Test]
        public void Test1()
        {
            bool isInvoked = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn = new(transport, 0);
            conn.OnForcedDisconnect += (_) => isInvoked = true;

            transport.ServerForceDisconnect(0);

            Assert.IsTrue(isInvoked);
        }

        [Test]
        public void Test2()
        {
            bool isInvoked1 = false;
            bool isInvoked2 = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn1 = new(transport, 1);
            conn1.OnForcedDisconnect += (_) => isInvoked1 = true;

            transport.ServerForceDisconnect(1);

            ConnectionToClient conn2 = new(transport, 2);
            conn2.OnForcedDisconnect += (_) => isInvoked2 = true;

            transport.ServerForceDisconnect(2);

            Assert.IsTrue(isInvoked1);
            Assert.IsTrue(isInvoked2);
        }

        [Test]
        public void Test3()
        {
            bool isInvoked1 = false;
            bool isInvoked2 = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn1 = new(transport, 1);
            conn1.OnForcedDisconnect += (_) => isInvoked1 = true;


            ConnectionToClient conn2 = new(transport, 2);
            conn2.OnForcedDisconnect += (_) => isInvoked2 = true;

            transport.ServerForceDisconnect(1);
            transport.ServerForceDisconnect(2);

            Assert.IsTrue(isInvoked1);
            Assert.IsTrue(isInvoked2);
        }

        [Test]
        public void Test4()
        {
            bool isInvoked1 = false;
            bool isInvoked2 = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn1 = new(transport, 1);
            conn1.OnForcedDisconnect += (_) => isInvoked1 = true;


            ConnectionToClient conn2 = new(transport, 2);
            conn2.OnForcedDisconnect += (_) => isInvoked2 = true;

            transport.ServerForceDisconnect(2);
            transport.ServerForceDisconnect(1);

            Assert.IsTrue(isInvoked1);
            Assert.IsTrue(isInvoked2);
        }

        [Test]
        public void Test5()
        {
            bool isInvoked1 = false;
            bool isInvoked2 = false;
            bool isInvoked3 = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn1 = new(transport, 1);
            conn1.OnForcedDisconnect += (_) => isInvoked1 = true;


            ConnectionToClient conn2 = new(transport, 2);
            conn2.OnForcedDisconnect += (_) => isInvoked2 = true;

            transport.ServerForceDisconnect(2);
            transport.ServerForceDisconnect(1);

            ConnectionToClient conn3 = new(transport, 3);
            conn3.OnForcedDisconnect += (_) => isInvoked3 = true;
            
            transport.ServerForceDisconnect(3);

            Assert.IsTrue(isInvoked1);
            Assert.IsTrue(isInvoked2);
            Assert.IsTrue(isInvoked3);
        }

        [Test]
        public void Test6()
        {
            bool isInvoked1 = false;
            bool isInvoked2 = false;
            bool isInvoked3 = false;
            
            ServerTransport transport = new();

            ConnectionToClient conn1 = new(transport, 1);
            conn1.OnForcedDisconnect += (_) => isInvoked1 = true;

            ConnectionToClient conn2 = new(transport, 2);
            conn2.OnForcedDisconnect += (_) => isInvoked2 = true;

            transport.ServerForceDisconnect(1);
            transport.ServerForceDisconnect(2);

            ConnectionToClient conn3 = new(transport, 3);
            conn3.OnForcedDisconnect += (_) => isInvoked3 = true;

            transport.ServerForceDisconnect(3);

            Assert.IsTrue(isInvoked1);
            Assert.IsTrue(isInvoked2);
            Assert.IsTrue(isInvoked3);
        }
    }
}
