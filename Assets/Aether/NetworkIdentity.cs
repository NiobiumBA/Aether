using Aether.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Aether
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class NetworkIdentity : MonoBehaviour
    {
        public enum InitializationState
        {
            None, AsPrefab, OnScene
        }

        public readonly struct SceneIdentityInfo : IEquatable<SceneIdentityInfo>
        {
            private readonly NetworkRoom m_room;
            private readonly uint m_netId;

            public readonly NetworkRoom Room => m_room;
            public readonly uint NetId => m_netId;

            public SceneIdentityInfo(NetworkRoom room, uint netId)
            {
                m_room = room;
                m_netId = netId;
            }

            public readonly override bool Equals(object obj)
            {
                return obj is SceneIdentityInfo other && Equals(other);
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(m_room, m_netId);
            }

            public readonly override string ToString()
            {
                return $"(Room: ({Room}), NetId: {NetId})";
            }

            public readonly bool Equals(SceneIdentityInfo other)
            {
                return m_room == other.m_room && m_netId == other.m_netId;
            }
        }

        public interface IReadOnlyRoomDictionary : IReadOnlyDictionary<SceneIdentityInfo, NetworkIdentity>
        {
        }

        public class RoomDictionary : Dictionary<SceneIdentityInfo, NetworkIdentity>, IReadOnlyRoomDictionary
        {
        }

        private static RoomDictionary s_roomIdentities = new();
        private static Dictionary<uint, NetworkIdentity> s_assetIdentities;

        public static IReadOnlyDictionary<uint, NetworkIdentity> AssetIdentities
        {
            // TODO Calling does not load all assets in memory.
            get
            {
                // In editor always find new assets.
#if UNITY_EDITOR
                s_assetIdentities = GenerateAssetDictionary();
#else
                if (s_assetIdentities == null)
                    s_assetIdentities = GenerateAssetDictionary();
#endif
                return s_assetIdentities;
            }
        }

        public static IReadOnlyRoomDictionary RoomIdentities => s_roomIdentities;

        public static uint GetSceneUniqueNetId()
        {
            IEnumerable<uint> netIds = s_roomIdentities.Where((pair) => pair.Value.InitState != InitializationState.None)
                                                       .Select((pair) => pair.Key.NetId);
            uint idNew = 1;

            while (netIds.Contains(idNew))
                idNew++;

            return idNew;
        }

        private static Dictionary<uint, NetworkIdentity> GenerateAssetDictionary()
        {
            NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>();

            Dictionary<uint, NetworkIdentity>  result = identities.Where(identity => identity.InitState == InitializationState.AsPrefab)
                                                                  .ToDictionary(identity => identity.NetId);
            return result;
        }

        // Use attributes to force Unity save changes in the editor.
        [SerializeField, HideInInspector] private uint m_netId = 0;
        [SerializeField, HideInInspector] private InitializationState m_initState = InitializationState.None;

        private NetworkBehaviour[] m_components;
        private NetworkRoom m_room;

        public uint NetId => m_netId;
        public InitializationState InitState => m_initState;
        public IReadOnlyList<NetworkBehaviour> Components => m_components;

        public NetworkRoom Room => m_room;

        public void InitializeOnScene(uint netId)
        {
            m_netId = netId;

            s_roomIdentities.Add(new SceneIdentityInfo(Room, m_netId), this);

            m_initState = InitializationState.OnScene;
        }

        private void Awake()
        {
            if (Application.isPlaying == false)
                return;

            if (m_initState == InitializationState.None)
            {
                DebugNotValidated();
                return;
            }

            FindRoom();

            SceneIdentityInfo key = new(Room, m_netId);

            if (m_initState == InitializationState.OnScene &&
                s_roomIdentities.Values.Contains(this) == false &&
                s_roomIdentities.ContainsKey(key) == false)
            {
                s_roomIdentities.Add(key, this);
            }

            InitializeComponents();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying == false)
                return;

            SceneIdentityInfo key = new(Room, m_netId);
            s_roomIdentities.Remove(key);
        }

        private void InitializeComponents()
        {
            m_components = GetComponents<NetworkBehaviour>();

            if (m_components.Length > byte.MaxValue)
                throw new NotSupportedException($"Too many {nameof(NetworkBehaviour)} scripts on object {name}.");

            for (int i = 0; i < m_components.Length; i++)
            {
                NetworkBehaviour component = m_components[i];

                component.Identity = this;
                component.ComponentId = (byte)i;
            }
        }

        private void FindRoom()
        {
            List<NetworkRoom> rooms = new();
            NetworkRoomManager.GetComponentsOnScene<NetworkRoom>(gameObject.scene, ref rooms);

            if (rooms.Count == 0)
                throw new Exception($"There are no {nameof(NetworkRoom)} scripts on the scene");

            m_room = rooms[0];
        }

        private void DebugNotValidated()
        {
            Debug.LogError($"Object {name} is not validated.", this);
            Debug.Log("Please, open this object in the inspector.");
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying)
                return;

            NetworkIdentity[] sceneIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            RegenerateUninitialized(sceneIdentities);
            RegenerateRepeated(sceneIdentities);

            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            GameObject currentPrefab = prefabStage == null ? null : prefabStage.prefabContentsRoot;

            NetworkIdentity[] assetIdentities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                .Where(identity => identity.gameObject != currentPrefab).ToArray();

            RegenerateUninitialized(assetIdentities);
            RegenerateRepeated(assetIdentities);
        }

        private void RegenerateUninitialized(NetworkIdentity[] identities)
        {
            foreach (NetworkIdentity identity in identities)
            {
                if (identity.InitState == InitializationState.None)
                    identity.GenerateId();
            }
        }

        private void RegenerateRepeated(NetworkIdentity[] identities)
        {
            for (int i = 0; i < identities.Length - 1; i++)
            {
                for (int j = i + 1; j < identities.Length; j++)
                {
                    if (identities[i].NetId == identities[j].NetId)
                        identities[i].GenerateId();
                }
            }
        }

        [ContextMenu("Regenerate ID")]
        private void GenerateId()
        {
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                if (m_initState != InitializationState.None)
                    return;

                DebugNotValidated();
                return;
            }

            bool isPrefab = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);

            if (isPrefab)
            {
                GenerateAssetUniqueNetId();
                m_initState = InitializationState.AsPrefab;
            }
            else
            {
                UnityEditor.Undo.RecordObject(this, $"Generated netId");

                GenerateSceneUniqueNetId();
                m_initState = InitializationState.OnScene;
            }
        }

        private void GenerateAssetUniqueNetId()
        {
            NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>();
            m_netId = GetUniqueId(identities);
        }

        private void GenerateSceneUniqueNetId()
        {
            NetworkIdentity[] identities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
            m_netId = GetUniqueId(identities);
        }

        private uint GetUniqueId(NetworkIdentity[] identities)
        {
            IEnumerable<uint> netIds = identities.Where(identity => identity != this)
                                                 .Select(identity => identity.NetId);
            uint idNew = 1;

            while (netIds.Contains(idNew))
                idNew++;

            return idNew;
        }
#endif
    }
}