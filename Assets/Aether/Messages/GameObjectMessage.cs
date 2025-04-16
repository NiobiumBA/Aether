using Aether.SceneManagement;
using UnityEngine;

namespace Aether.Messages
{
    public readonly struct GameObjectMessage : INetworkMessage
    {
        private enum GameObjectType : byte
        {
            Null = 0, OnScene = 1, Prefab = 2
        }

        private readonly GameObjectType m_type;
        private readonly uint m_roomId;
        private readonly uint m_id;

        public readonly uint RoomId => m_roomId;

        public readonly uint NetId => m_id;

        public readonly NetworkIdentity Identity
        {
            get
            {
                if (m_type == GameObjectType.Null)
                    return null;

                if (m_type == GameObjectType.OnScene)
                {
                    NetworkRoom room = NetworkRoomManager.GetRoomByNetId(m_roomId);

                    if (room == null)
                        throw new InvalidNetworkDataException($"There is no room with id: {m_roomId}");

                    NetworkIdentity.SceneIdentityInfo key = new(room, m_id);

                    if (NetworkIdentity.RoomIdentities.TryGetValue(key, out NetworkIdentity identity))
                        return identity;

                    throw new InvalidNetworkDataException($"There is no identity with id: {m_id} in the room: ({room})");
                }

                if (m_type == GameObjectType.Prefab)
                {
                    if (NetworkIdentity.AssetIdentities.TryGetValue(m_id, out NetworkIdentity identity))
                        return identity;

                    throw new InvalidNetworkDataException($"There is no asset with id: {m_id}");
                }
                
                throw new InvalidNetworkDataException(nameof(m_type));
            }
        }

        public readonly GameObject Object => Identity == null ? null : Identity.gameObject;

        public GameObjectMessage(GameObject gameObject)
        {
            if (gameObject == null)
            {
                m_type = GameObjectType.Null;
                m_roomId = 0;
                m_id = 0;
                return;
            }

            if (gameObject.TryGetComponent(out NetworkIdentity identity) == false)
                ThrowHelper.GameObjectNotIdentifiable(gameObject.name);

            (m_type, m_roomId, m_id) = GetFields(identity);
        }

        public GameObjectMessage(NetworkIdentity identity)
        {
            if (identity == null)
            {
                m_type = GameObjectType.Null;
                m_roomId = 0;
                m_id = 0;
                return;
            }

            (m_type, m_roomId, m_id) = GetFields(identity);
        }

        private static (GameObjectType, uint, uint) GetFields(NetworkIdentity identity)
        {
            if (identity.InitState == NetworkIdentity.InitializationState.None)
                ThrowHelper.GameObjectNotInitialized(identity.name);

            bool isPrefab = identity.InitState == NetworkIdentity.InitializationState.AsPrefab;

            GameObjectType type = isPrefab ? GameObjectType.Prefab : GameObjectType.OnScene;

            uint roomId = isPrefab ? 0 : identity.Room.NetId;

            uint id = identity.NetId;

            return (type, roomId, id);
        }
    }
}
