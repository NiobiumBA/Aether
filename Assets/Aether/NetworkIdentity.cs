using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

        private static Dictionary<uint, NetworkIdentity> s_sceneIdentities = new();
        private static Dictionary<uint, NetworkIdentity> s_assetIdentities;
        private static bool s_sceneEventsSubscribed = false;

        public static IReadOnlyDictionary<uint, NetworkIdentity> AssetIdentities
        {
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

        public static IReadOnlyDictionary<uint, NetworkIdentity> SceneIdentities => s_sceneIdentities;

        public static uint GetSceneUniqueNetId()
        {
            IEnumerable<uint> netIds = SceneIdentities.Where((pair) => pair.Value.InitState != InitializationState.None)
                                                      .Select((pair) => pair.Key);
            uint idNew = 1;

            while (netIds.Contains(idNew))
                idNew++;

            return idNew;
        }

        private static Dictionary<uint, NetworkIdentity> GenerateAssetDictionary()
        {
            NetworkIdentity[] identities = Resources.FindObjectsOfTypeAll<NetworkIdentity>();

            return identities.Where(identity => identity.InitState == InitializationState.AsPrefab)
                             .ToDictionary(identity => identity.AssetId);
        }

        private static void SubscribeToSceneEvents()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            s_sceneEventsSubscribed = true;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            s_sceneIdentities = new Dictionary<uint, NetworkIdentity>();
        }

        // Use attributes to force Unity save changes in the editor.
        [SerializeField, HideInInspector] private uint m_sceneId = 0;
        [SerializeField, HideInInspector] private uint m_assetId = 0;
        [SerializeField, HideInInspector] private InitializationState m_initState = InitializationState.None;
        private NetworkBehaviour[] m_components;

        public uint SceneId => m_sceneId;
        public uint AssetId => m_assetId;
        public InitializationState InitState => m_initState;
        public IReadOnlyList<NetworkBehaviour> Components => m_components;

        internal void InitializeOnScene(uint netId)
        {
            m_sceneId = netId;

            s_sceneIdentities.Add(m_sceneId, this);

            m_initState = InitializationState.OnScene;
        }

        private void Awake()
        {
            if (Application.isPlaying == false)
                return;

            if (s_sceneEventsSubscribed == false)
                SubscribeToSceneEvents();

            if (m_initState == InitializationState.None)
            {
                DebugNotValidated();
                return;
            }

            if (m_initState == InitializationState.OnScene &&
                s_sceneIdentities.Values.Contains(this) == false &&
                s_sceneIdentities.ContainsKey(m_sceneId) == false)
            {
                s_sceneIdentities.Add(m_sceneId, this);
            }

            InitializeComponents();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying == false)
                return;

            s_sceneIdentities.Remove(SceneId);
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
            RegenerateRepeated(sceneIdentities, true);

            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            GameObject currentPrefab = prefabStage == null ? null : prefabStage.prefabContentsRoot;

            NetworkIdentity[] assetIdentities = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                .Where(identity => identity.gameObject != currentPrefab).ToArray();

            RegenerateUninitialized(assetIdentities);
            RegenerateRepeated(assetIdentities, false);
        }

        private void RegenerateUninitialized(NetworkIdentity[] identities)
        {
            foreach (NetworkIdentity identity in identities)
            {
                if (identity.InitState == InitializationState.None)
                    identity.GenerateId();
            }
        }

        private void RegenerateRepeated(NetworkIdentity[] identities, bool useSceneId)
        {
            for (int i = 0; i < identities.Length - 1; i++)
            {
                for (int j = i + 1; j < identities.Length; j++)
                {
                    bool equalsSceneId = identities[i].SceneId == identities[j].SceneId;
                    bool equalsAssetId = identities[i].AssetId == identities[j].AssetId;

                    if (useSceneId)
                    {
                        if (equalsSceneId)
                            identities[i].GenerateId();
                    }
                    else
                    {
                        if (equalsAssetId)
                            identities[i].GenerateId();
                    }
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
            IEnumerable<uint> netIds = Resources.FindObjectsOfTypeAll<NetworkIdentity>()
                                                .Where(identity => identity != this)
                                                .Select(identity => identity.AssetId);

            m_assetId = GetUniqueId(netIds);
        }

        private void GenerateSceneUniqueNetId()
        {
            NetworkIdentity[] arrayIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            IEnumerable<uint> netIds = arrayIdentities.Where(identity => identity != this)
                                                      .Select(identity => identity.SceneId);

            m_sceneId = GetUniqueId(netIds);
        }

        private static uint GetUniqueId(IEnumerable<uint> netIds)
        {
            uint idNew = 1;

            while (netIds.Contains(idNew))
                idNew++;

            return idNew;
        }
#endif
    }
}