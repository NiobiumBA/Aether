using Aether.Connections;
using Aether.Messages;
using Aether.SceneManagement;
using Aether.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aether
{
    /// <summary>
    /// Contains static methods of GameObjects interactions.
    /// </summary>
    public class NetworkGameObjects : SingletonBehaviour<NetworkGameObjects>
    {
        private struct SpawnMessage : INetworkMessage
        {
            public GameObjectMessage originalGameObjectMessage;
            public uint netId;
            public Vector3 position;
            public Quaternion rotation;
            public GameObjectMessage parentGameObjectMessage;
        }

        private struct DestroyMessage : INetworkMessage
        {
            public GameObjectMessage gameObjectMessage;
        }

        private struct ChangeGameObjectActiveMessage : INetworkMessage
        {
            public GameObjectMessage gameObjectMessage;
            public BoolMessage isActive;
        }

        private struct ChangeBehaviourEnabledMessage : INetworkMessage
        {
            public BehaviourMessage behaviourMessage;
            public BoolMessage enabled;
        }

        private static Dictionary<(uint, NetworkRoom), SpawnMessage> s_spawnedObjectsMessages = new();
        private static Dictionary<(uint, NetworkRoom), ChangeGameObjectActiveMessage> s_gameObjectActiveMessages = new();
        private static Dictionary<(uint, NetworkRoom), ChangeBehaviourEnabledMessage> s_behaviourEnabledMessages = new();
        private static Dictionary<(uint, NetworkRoom), DestroyMessage> s_destroyedObjectMessages = new();

        private static bool s_clientCallbacksRegistered = false;
        private static bool s_eventsSubscribed = false;

        // TODO Spawn by NetworkRoom instead of parent

        /// <summary>
        /// Spawn NetworkIdentity on a scene with parent
        /// </summary>
        /// <returns>Spawned NetworkIdentity</returns>
        public static NetworkIdentity Spawn(NetworkIdentity original, Vector3 position, Quaternion rotation, NetworkIdentity parent)
        {
            ThrowHelper.ThrowIfNotServer(nameof(Spawn));

            ThrowHelper.ThrowIfNull(original, nameof(original));

            NetworkIdentity spawned = Instantiate(original, position, rotation, parent.transform);

            spawned.InitializeOnScene(NetworkIdentity.GetSceneUniqueNetId());

            SpawnMessage message = new()
            {
                originalGameObjectMessage = new GameObjectMessage(original.gameObject),
                netId = spawned.NetId,
                position = position,
                rotation = rotation,
                parentGameObjectMessage = new GameObjectMessage(parent)
            };

            s_spawnedObjectsMessages.Add((spawned.NetId, spawned.Room), message);

            SendMessageAll(spawned.Room, message);

            return spawned;
        }

        public static NetworkIdentity Spawn(NetworkIdentity original, NetworkIdentity parent)
        {
            return Spawn(original, Vector3.zero, Quaternion.identity, parent);
        }

        public static T Spawn<T>(T original, Vector3 position, Quaternion rotation, NetworkIdentity parent)
            where T : NetworkBehaviour
        {
            NetworkIdentity spawnedGameObject = Spawn(original.Identity, position, rotation, parent);

            return spawnedGameObject.GetComponent<T>();
        }

        public static T Spawn<T>(T original, NetworkIdentity parent)
            where T : NetworkBehaviour
        {
            return Spawn(original, Vector3.zero, Quaternion.identity, parent);
        }

        public static void ServerDestroy(NetworkIdentity identity)
        {
            ThrowHelper.ThrowIfNotServer(nameof(ServerDestroy));

            ThrowHelper.ThrowIfNull(identity, nameof(identity));

            GameObjectMessage gameObjectMessage = new(identity);

            DestroyMessage message = new()
            {
                gameObjectMessage = gameObjectMessage
            };

            (uint, NetworkRoom) key = (identity.NetId, identity.Room);

            if (s_spawnedObjectsMessages.Remove(key) == false)
                s_destroyedObjectMessages.Add(key, message);

            s_gameObjectActiveMessages.Remove(key);
            s_behaviourEnabledMessages.Remove(key);

            Destroy(identity);

            SendMessageAll(identity.Room, message);
        }

        public static void SetActive(GameObject obj, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetActive));

            ThrowHelper.ThrowIfNull(obj, nameof(obj));

            GameObjectMessage gameObjectMessage = new(obj);

            if (gameObjectMessage.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(obj));

            ChangeGameObjectActiveMessage message = new()
            {
                gameObjectMessage = gameObjectMessage,
                isActive = value
            };

            obj.SetActive(value);

            NetworkRoom room = gameObjectMessage.Identity.Room;

            s_gameObjectActiveMessages[(gameObjectMessage.NetId, room)] = message;

            SendMessageAll(room, message);
        }

        public static void SetActiveForClient(ConnectionToClient connection, GameObject obj, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetActiveForClient));

            ThrowHelper.ThrowIfNull(obj, nameof(obj));

            GameObjectMessage gameObjectMessage = new(obj);

            if (gameObjectMessage.Identity.Room.Connections.Contains(connection) == false)
                ThrowHelper.ConnectionObjIncorrect(obj);

            if (gameObjectMessage.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(obj));

            ChangeGameObjectActiveMessage message = new()
            {
                gameObjectMessage = gameObjectMessage,
                isActive = value
            };

            NetworkDispatcher.SendMessageByConnection(connection, message);
        }

        public static void SetEnabled(NetworkBehaviour behaviour, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetEnabled));

            ThrowHelper.ThrowIfNull(behaviour, nameof(behaviour));

            if (behaviour.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(behaviour));

            BehaviourMessage behaviourMessage = new(behaviour);

            ChangeBehaviourEnabledMessage message = new()
            {
                behaviourMessage = behaviourMessage,
                enabled = value
            };

            behaviour.enabled = value;

            NetworkRoom room = behaviour.Identity.Room;

            s_behaviourEnabledMessages[(behaviour.Identity.NetId, room)] = message;

            SendMessageAll(room, message);
        }

        public static void SetEnabledForClient(ConnectionToClient connection, NetworkBehaviour behaviour, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetEnabledForClient));

            ThrowHelper.ThrowIfNull(behaviour, nameof(behaviour));

            if (behaviour.Identity.Room.Connections.Contains(connection) == false)
                ThrowHelper.ConnectionObjIncorrect(behaviour);

            if (behaviour.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(behaviour));

            BehaviourMessage behaviourMessage = new(behaviour);

            ChangeBehaviourEnabledMessage message = new()
            {
                behaviourMessage = behaviourMessage,
                enabled = value
            };

            NetworkDispatcher.SendMessageByConnection(connection, message);
        }

        private static void OnSpawnGameObject(SpawnMessage message)
        {
            GameObject spawned = Instantiate(message.originalGameObjectMessage.Object,
                                             message.position,
                                             message.rotation,
                                             message.parentGameObjectMessage.Object.transform);

            NetworkIdentity identity = spawned.GetComponent<NetworkIdentity>();

            identity.InitializeOnScene(message.netId);
        }

        private static void OnDestroyGameObject(DestroyMessage message)
        {
            GameObject obj = message.gameObjectMessage.Object;
            Destroy(obj);
        }

        private static void OnChangeGameObjectActive(ChangeGameObjectActiveMessage message)
        {
            GameObject obj = message.gameObjectMessage.Object;
            obj.SetActive(message.isActive);
        }

        private static void OnChangeEnabledBehaviour(ChangeBehaviourEnabledMessage message)
        {
            NetworkBehaviour behaviour = message.behaviourMessage.Component;
            behaviour.enabled = message.enabled;
        }

        private static void SendMessageAll<TMessage>(NetworkRoom room, TMessage message)
            where TMessage : unmanaged, INetworkMessage
        {
            foreach (ConnectionToClient conn in room.Connections)
            {
                NetworkDispatcher.SendMessageByConnection(conn, message);
            }
        }

        private static void SendMessagesByConnection<TMessage>(NetworkRoom room,
                                                               NetworkConnection connection,
                                                               IDictionary<(uint, NetworkRoom), TMessage> messages)
            where TMessage : unmanaged, INetworkMessage
        {
            foreach (KeyValuePair<(uint, NetworkRoom room), TMessage> pair in messages)
            {
                if (pair.Key.room == room)
                    NetworkDispatcher.SendMessageByConnection(connection, pair.Value);
            }
        }

        private static void RegisterClientCallbacks()
        {
            if (s_clientCallbacksRegistered)
                return;

            NetworkApplication.ClientDispatcher.RegisterMessageCallback<SpawnMessage>(OnSpawnGameObject);
            NetworkApplication.ClientDispatcher.RegisterMessageCallback<DestroyMessage>(OnDestroyGameObject);
            NetworkApplication.ClientDispatcher.RegisterMessageCallback<ChangeGameObjectActiveMessage>(OnChangeGameObjectActive);
            NetworkApplication.ClientDispatcher.RegisterMessageCallback<ChangeBehaviourEnabledMessage>(OnChangeEnabledBehaviour);

            s_clientCallbacksRegistered = true;
        }

        private static void RemoveClientCallbacks()
        {
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<SpawnMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<DestroyMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<ChangeGameObjectActiveMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<ChangeBehaviourEnabledMessage>();

            s_clientCallbacksRegistered = false;
        }

        private static void SendInitDataAll(NetworkRoom room, ConnectionToClient connection)
        {
            foreach (SyncObject syncObj in SyncObject.AllSyncObjects)
            {
                if (syncObj.Owner.Identity.Room == room)
                {
                    syncObj.SendInitData(connection);
                }
            }
        }

        private static void OnAddClientToRoom(NetworkRoom room, ConnectionToClient connection)
        {
            if (connection is not LocalConnectionToClient)
            {
                SendMessagesByConnection(room, connection, s_spawnedObjectsMessages);
                SendMessagesByConnection(room, connection, s_gameObjectActiveMessages);
                SendMessagesByConnection(room, connection, s_behaviourEnabledMessages);
                SendMessagesByConnection(room, connection, s_destroyedObjectMessages);

                if (SyncObject.EventSystem.EnabledOnServer)
                    SendInitDataAll(room, connection);
            }
        }

        private static void OnRoomLoad(NetworkRoom room)
        {
            room.OnAddClient += connection => OnAddClientToRoom(room, connection);
        }

        private void OnEnable()
        {
            if (s_eventsSubscribed)
                return;

            // Use static method to avoid double subscribing
            // because Awake invokes earlier then OnDisable
            NetworkRoomManager.OnRoomLoad += OnRoomLoad;

            s_eventsSubscribed = true;
        }

        protected override void Start()
        {
            base.Start();

            if (NetworkApplication.IsClientOnly)
                RegisterClientCallbacks();
        }

        private void OnDestroy()
        {
            if (ShouldBeDestroyed)
                return;
            
            if (NetworkApplication.IsClientOnly)
                RemoveClientCallbacks();

            if (NetworkApplication.IsServer)
            {
                s_spawnedObjectsMessages = new();
                s_gameObjectActiveMessages = new();
                s_behaviourEnabledMessages = new();
                s_destroyedObjectMessages = new();
            }
        }
    }
}
