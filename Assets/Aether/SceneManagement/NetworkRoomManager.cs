using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aether.SceneManagement
{
    public static class NetworkRoomManager
    {
        public static event Action<NetworkRoom> OnRoomLoad;
        public static event Action<NetworkRoom> OnRoomUnload;

        public static IReadOnlyCollection<NetworkRoom> LoadedRooms => s_loadedRooms.Values;

        private static readonly Dictionary<uint, NetworkRoom> s_loadedRooms = new();
        private static readonly CallbackQueue<int, Action<NetworkRoom>> s_loadCallbacks = new();

        /// <summary>
        /// </summary>
        /// <returns>Room by the scene if such exists, else null</returns>
        public static NetworkRoom GetRoom(Scene scene)
        {
            foreach (NetworkRoom room in s_loadedRooms.Values)
            {
                if (room.Scene == scene)
                    return room;
            }

            return null;
        }

        /// <summary>
        /// </summary>
        /// <returns>Room by netId if such exists, else null</returns>
        public static NetworkRoom GetRoomByNetId(uint netRoomId)
        {
            if (s_loadedRooms.TryGetValue(netRoomId, out NetworkRoom room))
                return room;

            return null;
        }

        public static void LoadRoomAsync(int buildId, LoadSceneMode loadSceneMode, Action<NetworkRoom> loadCallback)
        {
            s_loadCallbacks.Enqueue(buildId, loadCallback);

            LoadSceneParameters loadParameters = new(loadSceneMode, LocalPhysicsMode.Physics2D | LocalPhysicsMode.Physics3D);
            AsyncOperation operation = SceneManager.LoadSceneAsync(buildId, loadParameters);
            operation.allowSceneActivation = true;
        }

        /// <summary>
        /// Load room asynchronously with mode <see cref="LoadSceneMode.Additive"/>
        /// </summary>
        public static void LoadRoomAsync(int buildId, Action<NetworkRoom> loadCallback)
        {
            LoadRoomAsync(buildId, LoadSceneMode.Additive, loadCallback);
        }

        public static void LoadRoomAsync(int buildId, LoadSceneMode loadSceneMode)
        {
            LoadRoomAsync(buildId, loadSceneMode, null);
        }

        /// <summary>
        /// Load room asynchronously with mode <see cref="LoadSceneMode.Additive"/>
        /// </summary>
        public static void LoadRoomAsync(int buildId)
        {
            LoadRoomAsync(buildId, LoadSceneMode.Additive, null);
        }

        public static void UnloadRoomAsync(NetworkRoom room, Action unloadCallback = null)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(room.Scene);
            asyncOperation.completed += _ => unloadCallback?.Invoke();
        }

        public static void UnloadRoomAsync(uint netId, Action unloadCallback = null)
        {
            UnloadRoomAsync(s_loadedRooms[netId], unloadCallback);
        }

        public static void GetComponentsOnScene<T>(Scene scene, ref List<T> components)
            where T : Component
        {
            if (components == null)
                components = new List<T>();
            else
                components.Clear();

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                components.AddRange(rootObj.GetComponentsInChildren<T>());
            }
        }

        internal static void SceneRoomLoad(NetworkRoom room)
        {
            uint lastNetId = room.NetId;
            s_loadedRooms.Add(lastNetId, room);

            if (s_loadCallbacks.TryDequeue(room.BuildSceneId, out Action<NetworkRoom> callback) == false)
            {
                Debug.LogWarning($"Room ({room}) was loaded without callback");
                return;
            }

            callback?.Invoke(room);

            // action can modify NetId
            if (lastNetId != room.NetId)
            {
                s_loadedRooms.Remove(lastNetId);
                s_loadedRooms.Add(room.NetId, room);
            }

            OnRoomLoad?.Invoke(room);
        }

        internal static void SceneRoomUnload(NetworkRoom room)
        {
            s_loadedRooms.Remove(room.NetId);

            OnRoomUnload?.Invoke(room);
        }
    }
}
