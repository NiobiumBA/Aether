using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;

namespace Aether.Transports
{
    public class TcpTransport : NetworkTransport
    {
        public class ExceptionError : TransportError
        {
            public Exception Exception { get; private set; }

            public ExceptionError(Exception exception)
            {
                Exception = exception;
            }
        }

        public class ServerError : ExceptionError
        {
            public ServerError(Exception exception) : base(exception) { }
        }

        public class ServerConnectionError : ServerError
        {
            public uint ConnectionId { get; private set; }

            public ServerConnectionError(uint connectionId, Exception exception)
                : base(exception)
            {
                ConnectionId = connectionId;
            }
        }

        public class ClientError : ExceptionError
        {
            public ClientError(Exception exception) : base(exception) { }
        }

        private readonly object m_subscribeLock = new();
        private readonly object m_clientsLock = new();

        private TcpListener m_selfListener;
        private TcpClient m_selfClient;
        private bool m_serverStarted = false;

        private Dictionary<uint, TcpClient> m_clients = new();

        private ConcurrentQueue<Action> m_beforeHandleDataActions = new();

        private bool m_subscribedEventSystem = false;

        public bool ServerStarted => m_serverStarted;

        public bool ClientConnected => m_selfClient != null && m_selfClient.Client != null && m_selfClient.Connected;

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (ServerStarted)
                    return m_selfListener.LocalEndpoint as IPEndPoint;

                if (ClientConnected)
                    return m_selfClient.Client.LocalEndPoint as IPEndPoint;

                return null;
            }
        }

        public IPEndPoint RemoteEndPoint => ClientConnected ? m_selfClient.Client.RemoteEndPoint as IPEndPoint : null;

        public TcpTransport()
        {
        }

        public override int GetDataThreshold()
        {
            return 1024;
        }

        public override void ClientConnect(string address)
        {
            if (ServerStarted)
            {
                InvalidOperationException exception = new("The server already started");
                ClientError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            if (ClientConnected)
            {
                InvalidOperationException exception = new("The client already connected");
                ClientError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            if (ParseIPAndPort(address, out IPAddress ip, out int port) == false)
            {
                ArgumentException exception = new(nameof(address));
                ClientError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            m_selfClient?.Close();

            try
            {
                m_selfClient = new TcpClient();
            }
            catch (SocketException exception)
            {
                ClientError error = new(exception);

                OnTransportErrorEventInvoke(error);
                return;
            }

            SubscribeEventSystem();

            TcpClientConnectAsync(new IPEndPoint(ip, port));
        }

        public override void SendToClient(uint connectionId, ArraySegment<byte> data)
        {
            if (ServerStarted == false)
            {
                InvalidOperationException exception = new("Server not started");
                ServerConnectionError error = new(connectionId, exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            TcpClient client;

            lock (m_clientsLock)
            {
                if (m_clients.TryGetValue(connectionId, out client) == false)
                {
                    ArgumentException exception = new(nameof(connectionId));
                    ServerConnectionError error = new(connectionId, exception);
                    OnTransportErrorEventInvoke(error);
                    return;
                }
            }

            if (client.Connected == false)
                return; // Disconnection of client is handling in StreamReceiveDataAsync

            try
            {
                client.GetStream().Write(data.Array, data.Offset, data.Count);
            }
            catch (Exception exception)
            {
                ServerConnectionError error = new(connectionId, exception);

                OnTransportErrorEventInvoke(error);
            }
        }

        public override void SendToServer(ArraySegment<byte> data)
        {
            if (m_selfClient == null)
            {
                InvalidOperationException exception = new("The client is not connected");
                ClientError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            if (ClientConnected == false)
                return; // Disconnection of client is handling in StreamReceiveDataAsync

            try
            {
                m_selfClient.GetStream().Write(data.Array, data.Offset, data.Count);
            }
            catch (Exception exception)
            {
                ClientError error = new(exception);
                OnTransportErrorEventInvoke(error);
            }
        }

        public override void ClientDisconnect()
        {
            TcpClientDisconnect(DisconnectType.Itself);
        }

        public override void ServerDisconnect(uint connectionId)
        {
            TcpServerDisconnect(connectionId, DisconnectType.Itself);
        }

        public void StartServer(string address)
        {
            if (ParseIPAndPort(address, out IPAddress ip, out int port) == false)
            {
                ArgumentException exception = new(nameof(address));
                ServerError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }
            
            if (ServerStarted)
                return;

            try
            {
                m_selfListener = new TcpListener(ip, port);
                m_selfListener.Start();
            }
            catch (Exception exception)
            {
                ServerError error = new(exception);
                OnTransportErrorEventInvoke(error);
                return;
            }

            m_serverStarted = true;

            SubscribeEventSystem();
        }

        public void StopServer()
        {
            if (ServerStarted == false)
                return;

            m_selfListener.Stop();

            m_serverStarted = false;

            UnsubscribeEventSystem();
        }

        private async void TcpClientConnectAsync(IPEndPoint endPoint)
        {
            try
            {
                await m_selfClient.ConnectAsync(endPoint.Address, endPoint.Port);
            }
            catch (Exception exception)
            {
                m_selfClient?.Close();

                ClientError error = new(exception);

                m_beforeHandleDataActions.Enqueue(() => OnTransportErrorEventInvoke(error));

                return;
            }

            m_beforeHandleDataActions.Enqueue(OnClientConnectEventInvoke);

            StreamReceiveDataAsync(m_selfClient.GetStream(),
                                   () => TcpClientDisconnect(DisconnectType.Forced),
                                   OnClientDataReceiveEventInvoke);
        }

        private void TcpClientDisconnect(DisconnectType disconnectType)
        {
            if (ClientConnected == false)
            {
                m_selfClient.Close();
                return;
            }

            m_selfClient.Close();

            OnClientDisconnectEventInvoke(disconnectType);

            UnsubscribeEventSystem();
        }

        private void TcpServerDisconnect(uint connectionId, DisconnectType disconnect)
        {
            TcpClient client;

            lock (m_clientsLock)
            {
                if (m_clients.TryGetValue(connectionId, out client) == false)
                {
                    ArgumentException exception = new(nameof(connectionId));
                    ServerConnectionError error = new(connectionId, exception);
                    OnTransportErrorEventInvoke(error);
                    return;
                }

                m_clients.Remove(connectionId);
            }

            client.Close();

            OnServerDisconnectEventInvoke(connectionId, disconnect);
        }

        private void BeforeHandleData()
        {
            lock (m_subscribeLock)
            {
                if (m_subscribedEventSystem == false)
                {
                    NetworkEventSystem.BeforeHandleData -= BeforeHandleData;
                }
            }

            ActionsInvoke();

            if (ServerStarted)
                AcceptAllClientsOnServer();
        }

        private void AcceptAllClientsOnServer()
        {
            while (m_selfListener.Pending())
            {
                TcpClient newClient;

                try
                {
                    newClient = m_selfListener.AcceptTcpClient();
                }
                catch (Exception exception)
                {
                    ServerError error = new(exception);

                    OnTransportErrorEventInvoke(error);

                    StopServer();
                    return;
                }

                uint connectionId;

                lock (m_clientsLock)
                {
                    connectionId = GetConnectionId();

                    m_clients.Add(connectionId, newClient);
                }

                OnServerConnectEventInvoke(connectionId);

                StreamReceiveDataAsync(newClient.GetStream(),
                                       () => TcpServerDisconnect(connectionId, DisconnectType.Forced),
                                       (data) => OnServerDataReceiveEventInvoke(connectionId, data));
            }
        }

        private async void StreamReceiveDataAsync(NetworkStream stream,
                                                  Action disconnectAction,
                                                  Action<ArraySegment<byte>> receiveAction)
        {
            byte[] buffer = new byte[GetDataThreshold()];

            while (true)
            {
                int bytesRead;

                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (IOException exception)
                {
                    if (exception.InnerException is not SocketException)
                    {
                        ClientError error = new(exception);
                        m_beforeHandleDataActions.Enqueue(() => OnTransportErrorEventInvoke(error));
                    }

                    m_beforeHandleDataActions.Enqueue(disconnectAction);
                    return;
                }
                catch (Exception exception)
                {
                    ClientError error = new(exception);

                    m_beforeHandleDataActions.Enqueue(() => OnTransportErrorEventInvoke(error));

                    m_beforeHandleDataActions.Enqueue(disconnectAction);
                    return;
                }

                if (bytesRead == 0)
                {
                    m_beforeHandleDataActions.Enqueue(disconnectAction);
                    return;
                }

                byte[] tempBuffer = new byte[bytesRead];

                Array.Copy(buffer, tempBuffer, bytesRead);

                ArraySegment<byte> data = new(tempBuffer);

                m_beforeHandleDataActions.Enqueue(() => receiveAction(data));
            }
        }

        private void ActionsInvoke()
        {
            while (m_beforeHandleDataActions.TryDequeue(out Action action))
            {
                action();
            }
        }

        private void SubscribeEventSystem()
        {
            lock (m_subscribeLock)
            {
                if (m_subscribedEventSystem == false)
                {
                    NetworkEventSystem.BeforeHandleData += BeforeHandleData;

                    m_subscribedEventSystem = true;
                }
            }
        }

        private void UnsubscribeEventSystem()
        {
            lock (m_subscribeLock)
            {
                if (m_clients.Count == 0)
                {
                    m_subscribedEventSystem = false;
                }
            }
        }

        private uint GetConnectionId()
        {
            return m_clients.Count == 0 ? 0 : m_clients.Keys.Max() + 1;
        }

        private static bool ParseIPAndPort(string address, out IPAddress ip, out int port)
        {
            try
            {
                string[] pair = address.Split(':');

                if (pair.Length != 2)
                {
                    ip = null;
                    port = 0;

                    return false;
                }

                ip = IPAddress.Parse(pair[0]);
                port = int.Parse(pair[1]);

                return true;
            }
            catch
            {
                ip = null;
                port = 0;

                return false;
            }
        }
    }
}
