using Aether.Connections;
using Aether.Messages;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Aether.SceneManagement
{
    public class NetworkRoom : NetworkBehaviour
    {
        [NetworkMessageName("AddClient")]
        private struct AddClientMessage : INetworkMessage
        {
            public int buildId;
            public uint roomNetId;
        }

        [NetworkMessageName("ClientLoadCallback")]
        private struct ClientLoadCallbackMessage : INetworkMessage
        {
            public uint roomNetId;
        }

        [NetworkMessageName("RemoveClient")]
        private struct RemoveClientMessage : INetworkMessage
        {
            public uint roomNetId;
        }

        [NetworkMessageName("ClientUnloadCallback")]
        private struct ClientUnloadCallbackMessage : INetworkMessage
        {
            public uint roomNetId;
        }


        private static uint s_maxRoomId = 0;

        private static bool s_serverHandlersRegistered = false;
        private static bool s_clientHandlersRegistered = false;

        public event Action<ConnectionToClient> OnAddClient;
        public event Action<ConnectionToClient> OnRemoveClient;

        private readonly List<ConnectionToClient> m_clients = new();
        private uint m_netId;

        public uint NetId => m_netId;

        public int BuildSceneId => Scene.buildIndex;

        public Scene Scene => gameObject.scene;

        public IReadOnlyList<ConnectionToClient> Clients => m_clients;


        private static void AddOnClientHandler(AddClientMessage message)
        {
            NetworkRoomManager.LoadRoomAsync(message.buildId, LoadSceneMode.Single,
                room => room.OnClientLoadInMemory(message.roomNetId));
        }

        private void OnClientLoadInMemory(uint roomNetId)
        {
            if (NetworkApplication.ClientConnected == false)
                return;

            m_netId = roomNetId;
            s_maxRoomId = Math.Max(m_netId, s_maxRoomId);

            ClientLoadCallbackMessage message = new()
            {
                roomNetId = roomNetId
            };

            NetworkApplication.ClientDispatcher.SendMessage(message);
        }

        private static void ClientLoadCallbackHandler(NetworkConnection connection, ClientLoadCallbackMessage message)
        {
            NetworkRoom room = NetworkRoomManager.GetRoomByNetId(message.roomNetId);

            connection.OnSelfDisconnect += room.OnDisconnectAddition;
            connection.OnForcedDisconnect += room.OnDisconnectAddition;

            ConnectionToClient connToClient = connection as ConnectionToClient;

            room.m_clients.Add(connToClient);
            room.OnAddClient?.Invoke(connToClient);
        }

        private static void RemoveOnClientHandler(RemoveClientMessage message)
        {
            NetworkRoomManager.UnloadRoomAsync(message.roomNetId,
                () => OnUnloadFromMemory(message.roomNetId));
        }

        private static void OnUnloadFromMemory(uint roomNetId)
        {
            if (NetworkApplication.ClientConnected == false)
                return;

            ClientUnloadCallbackMessage message = new()
            {
                roomNetId = roomNetId
            };

            NetworkApplication.ClientDispatcher.SendMessage(message);
        }

        private static void ClientUnloadCallbackHandler(NetworkConnection connection, ClientUnloadCallbackMessage message)
        {
            NetworkRoom room = NetworkRoomManager.GetRoomByNetId(message.roomNetId);

            connection.OnSelfDisconnect -= room.OnDisconnectAddition;
            connection.OnForcedDisconnect -= room.OnDisconnectAddition;

            ConnectionToClient connToClient = connection as ConnectionToClient;

            room.m_clients.Remove(connToClient);
            room.OnRemoveClient?.Invoke(connToClient);
        }

        public void AddClient(ConnectionToClient connection)
        {
            ThrowHelper.ThrowIfNotServer(nameof(AddClient));

            if (connection.IsActive == false)
                ThrowHelper.ArgumentInactiveConnection(nameof(connection));

            if (m_clients.Contains(connection))
                throw new ArgumentException("Connection has already been added", nameof(connection));

            if (connection is LocalConnectionToClient)
            {
                m_clients.Add(connection);
                OnAddClient?.Invoke(connection);
                return;
            }

            AddClientMessage message = new()
            {
                buildId = BuildSceneId,
                roomNetId = NetId
            };

            NetworkDispatcher.SendMessageByConnection(connection, message);
        }

        public void RemoveClient(ConnectionToClient connection)
        {
            ThrowHelper.ThrowIfNotServer(nameof(AddClient));

            if (m_clients.Contains(connection) == false)
                throw new ArgumentException("Connection has not been added", nameof(connection));

            if (connection is LocalConnectionToClient)
            {
                m_clients.Remove(connection);
                OnRemoveClient?.Invoke(connection);
                return;
            }

            RemoveClientMessage message = new()
            {
                roomNetId = NetId
            };

            NetworkDispatcher.SendMessageByConnection(connection, message);
        }

        public override string ToString()
        {
            return $"{Scene.name}: {m_netId}";
        }

        private void Awake()
        {
            SingletonCheck();

            if (NetworkApplication.IsClient)
                RegisterHandlersOnClient();

            if (NetworkApplication.IsServer)
                RegisterHandlersOnServer();

            m_netId = s_maxRoomId++;

            NetworkRoomManager.SceneRoomLoad(this);
        }

        private void OnDestroy()
        {
            ConnectionToClient[] tempConnections = new ConnectionToClient[m_clients.Count];
            m_clients.CopyTo(tempConnections, 0);

            foreach (ConnectionToClient conn in tempConnections)
            {
                if (conn.IsActive)
                    RemoveClient(conn);
            }

            NetworkRoomManager.SceneRoomUnload(this);

            if (NetworkApplication.IsClient)
                RemoveHandlersOnClient();

            if (NetworkApplication.IsServer)
                RemoveHandlersOnServer();
        }

        private void SingletonCheck()
        {
            List<NetworkRoom> components = null;
            NetworkRoomManager.GetComponentsOnScene<NetworkRoom>(Scene, ref components);

            if (components.Count > 1)
                ThrowHelper.TooManyScriptsOnScene(nameof(NetworkRoom));
        }

        private void OnDisconnectAddition(NetworkConnection connection)
        {
            connection.OnForcedDisconnect -= OnDisconnectAddition;
            connection.OnSelfDisconnect -= OnDisconnectAddition;

            ConnectionToClient connToClient = connection as ConnectionToClient;

            m_clients.Remove(connToClient);
            OnRemoveClient?.Invoke(connToClient);
        }

        private void RegisterHandlersOnClient()
        {
            if (s_clientHandlersRegistered)
                return;

            NetworkApplication.ClientDispatcher.RegisterMessageCallback<AddClientMessage>(AddOnClientHandler);
            NetworkApplication.ClientDispatcher.RegisterMessageCallback<RemoveClientMessage>(RemoveOnClientHandler);

            s_clientHandlersRegistered = true;
        }

        private void RegisterHandlersOnServer()
        {
            if (s_serverHandlersRegistered)
                return;

            NetworkApplication.ServerDispatcher.RegisterMessageCallback<ClientLoadCallbackMessage>(ClientLoadCallbackHandler);
            NetworkApplication.ServerDispatcher.RegisterMessageCallback<ClientUnloadCallbackMessage>(ClientUnloadCallbackHandler);

            s_serverHandlersRegistered = true;
        }

        private void RemoveHandlersOnClient()
        {
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<AddClientMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<RemoveClientMessage>();

            s_clientHandlersRegistered = false;
        }

        private void RemoveHandlersOnServer()
        {
            NetworkApplication.ServerDispatcher.RemoveMessageCallback<ClientLoadCallbackMessage>();
            NetworkApplication.ServerDispatcher.RemoveMessageCallback<ClientUnloadCallbackMessage>();

            s_serverHandlersRegistered = false;
        }
    }
}
