using Aether.Connections;
using Aether.Messages;
using Aether.Synchronization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aether
{
    public class NetworkGameObjectInteractions : SingletonNetworkBehaviour<NetworkGameObjectInteractions>
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

        private static Dictionary<uint, SpawnMessage> s_spawnedObjectsMessages = new();
        private static Dictionary<uint, ChangeGameObjectActiveMessage> s_gameObjectActiveMessages = new();
        private static Dictionary<uint, ChangeBehaviourEnabledMessage> s_behaviourEnabledMessages = new();
        private static Dictionary<uint, DestroyMessage> s_destroyedObjectMessages = new();

        private static bool s_clientCallbacksRegistered = false;

        public static GameObject Spawn(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
        {
            ThrowHelper.ThrowIfNotServer(nameof(Spawn));

            if (original == null)
                throw new ArgumentNullException(nameof(original));

            GameObject spawned = Instantiate(original, position, rotation, parent);

            if (spawned.TryGetComponent(out NetworkIdentity spawnedIdentity) == false)
                ThrowHelper.GameObjectNotIdentifiable(spawned.name);

            spawnedIdentity.InitializeOnScene(NetworkIdentity.GetSceneUniqueNetId());

            GameObject parentGameObject = parent == null ? null : parent.gameObject;

            SpawnMessage message = new()
            {
                originalGameObjectMessage = new GameObjectMessage(original),
                netId = spawnedIdentity.SceneId,
                position = position,
                rotation = rotation,
                parentGameObjectMessage = new GameObjectMessage(parentGameObject)
            };

            s_spawnedObjectsMessages.Add(spawnedIdentity.SceneId, message);

            NetworkApplication.ServerDispatcher.SendMessageAllRemote(message);

            return spawned;
        }

        public static GameObject Spawn(GameObject original, Transform parent)
        {
            return Spawn(original, Vector3.zero, Quaternion.identity, parent);
        }

        public static T Spawn<T>(T original, Vector3 position, Quaternion rotation, Transform parent)
            where T : NetworkBehaviour
        {
            GameObject spawnedGameObject = Spawn(original.gameObject, position, rotation, parent);

            return spawnedGameObject.GetComponent<T>();
        }

        public static T Spawn<T>(T original, Transform parent)
            where T : NetworkBehaviour
        {
            return Spawn(original, Vector3.zero, Quaternion.identity, parent);
        }

        public static void ServerDestroy(GameObject obj)
        {
            ThrowHelper.ThrowIfNotServer(nameof(ServerDestroy));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            GameObjectMessage gameObjectMessage = new(obj);

            DestroyMessage message = new()
            {
                gameObjectMessage = gameObjectMessage
            };

            NetworkApplication.ServerDispatcher.SendMessageAllRemote(message);

            uint netId = gameObjectMessage.NetId;

            if (s_spawnedObjectsMessages.Remove(netId) == false)
                s_destroyedObjectMessages.Add(netId, message);

            s_gameObjectActiveMessages.Remove(netId);
            s_behaviourEnabledMessages.Remove(netId);

            Destroy(obj);
        }

        public static void SetActive(GameObject obj, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetActive));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            GameObjectMessage gameObjectMessage = new(obj);

            if (gameObjectMessage.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(obj));

            ChangeGameObjectActiveMessage message = new()
            {
                gameObjectMessage = gameObjectMessage,
                isActive = value
            };

            obj.SetActive(value);

            s_gameObjectActiveMessages.Add(gameObjectMessage.NetId, message);

            NetworkApplication.ServerDispatcher.SendMessageAllRemote(message);
        }

        public static void SetActiveForConnection(ConnectionToClient connection, GameObject obj, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetActiveForConnection));

            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            GameObjectMessage gameObjectMessage = new(obj);

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

            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

            if (behaviour.Identity.InitState == NetworkIdentity.InitializationState.AsPrefab)
                ThrowHelper.UnableChangePrefab(nameof(behaviour));

            BehaviourMessage behaviourMessage = new(behaviour);

            ChangeBehaviourEnabledMessage message = new()
            {
                behaviourMessage = behaviourMessage,
                enabled = value
            };

            behaviour.enabled = value;

            s_behaviourEnabledMessages.Add(behaviour.Identity.SceneId, message);

            NetworkApplication.ServerDispatcher.SendMessageAllRemote(message);
        }

        public static void SetEnabledForConnection(ConnectionToClient connection, NetworkBehaviour behaviour, bool value)
        {
            ThrowHelper.ThrowIfNotServer(nameof(SetEnabledForConnection));

            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

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
            GameObject parentGameObject = message.parentGameObjectMessage.Object;
            Transform parent = parentGameObject == null ? null : parentGameObject.transform;

            GameObject spawned = Instantiate(message.originalGameObjectMessage.Object,
                                             message.position,
                                             message.rotation,
                                             parent);

            if (spawned.TryGetComponent(out NetworkIdentity identity) == false)
                ThrowHelper.GameObjectNotIdentifiable(spawned.name);

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

        private static void SendMessagesByConnection<TMessage>(NetworkConnection connection, IEnumerable<TMessage> messages)
            where TMessage : unmanaged, INetworkMessage
        {
            foreach (TMessage message in messages)
            {
                NetworkDispatcher.SendMessageByConnection(connection, message);
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

        private static void UnsubscribeClientCallbacks()
        {
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<SpawnMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<DestroyMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<ChangeGameObjectActiveMessage>();
            NetworkApplication.ClientDispatcher.RemoveMessageCallback<ChangeBehaviourEnabledMessage>();

            s_clientCallbacksRegistered = false;
        }

        private static void SendInitDataAll()
        {
            foreach (SyncObject syncObj in SyncObject.AllSyncObjects)
            {
                SendInitDataAllRemote(syncObj);
            }
        }

        private static void SendInitDataAllRemote(SyncObject syncObj)
        {
            foreach (ConnectionToClient conn in NetworkApplication.ServerDispatcher.Connections)
            {
                if (conn is not LocalConnectionToClient)
                    syncObj.SendInitData(conn);
            }
        }

        protected internal override void OnConnect(NetworkConnection connection)
        {
            if (connection is ConnectionToClient and not LocalConnectionToClient)
            {
                SendMessagesByConnection(connection, s_spawnedObjectsMessages.Values);
                SendMessagesByConnection(connection, s_gameObjectActiveMessages.Values);
                SendMessagesByConnection(connection, s_behaviourEnabledMessages.Values);
                SendMessagesByConnection(connection, s_destroyedObjectMessages.Values);

                if (SyncObject.EventSystem.EnabledOnServer)
                    SendInitDataAll();
            }

            if (connection is ConnectionToServer and not LocalConnectionToServer)
            {
                RegisterClientCallbacks();
            }
        }

        protected override void Start()
        {
            base.Start();

            if (NetworkApplication.IsClientOnly)
                RegisterClientCallbacks();
        }

        private void OnDestroy()
        {
            if (NetworkApplication.IsClientOnly)
            {
                UnsubscribeClientCallbacks();
            }

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
