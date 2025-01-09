using System;
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
        private readonly uint m_id;

        public readonly uint NetId => m_id;

        public readonly NetworkIdentity Identity => m_type switch
        {
            GameObjectType.Null => null,
            GameObjectType.OnScene => NetworkIdentity.SceneIdentities[m_id],
            GameObjectType.Prefab => NetworkIdentity.AssetIdentities[m_id],
            _ => throw new InvalidOperationException()
        };

        public readonly GameObject Object
        {
            get
            {
                if (Identity == null)
                    return null;

                return Identity.gameObject;
            }
        }

        public GameObjectMessage(GameObject gameObject)
        {
            if (gameObject == null)
            {
                m_type = GameObjectType.Null;
                m_id = 0;
                return;
            }

            if (gameObject.TryGetComponent(out NetworkIdentity identity) == false)
                ThrowHelper.GameObjectNotIdentifiable(gameObject.name);

            if (identity.InitState == NetworkIdentity.InitializationState.None)
                ThrowHelper.GameObjectNotInitialized(gameObject.name);

            bool isPrefab = identity.InitState == NetworkIdentity.InitializationState.AsPrefab;

            m_type = isPrefab ? GameObjectType.Prefab : GameObjectType.OnScene;
            m_id = isPrefab ? identity.AssetId : identity.SceneId;
        }
    }
}
