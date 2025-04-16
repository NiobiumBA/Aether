using Aether.Synchronization;
using System;
using System.Runtime.InteropServices;

namespace Aether
{
    internal static class ThrowHelper
    {
        [UnityEngine.HideInCallstack]
        internal static void ThrowIfNull(object obj, string paramName)
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }

        [UnityEngine.HideInCallstack]
        internal static void ThrowIfNull(UnityEngine.Object obj, string paramName)
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }

        [UnityEngine.HideInCallstack]
        internal static void RepeatedMessageRegister(string messageHandlerName)
        {
            throw new ArgumentException($"A callback of message with name {messageHandlerName} already registered");
        }

        [UnityEngine.HideInCallstack]
        internal static void RepeatedHandlerRegister(string handlerName)
        {
            throw new ArgumentException($"A handler with name {handlerName} already registered");
        }

        [UnityEngine.HideInCallstack]
        internal static void ArgumentInactiveConnection(string paramName)
        {
            throw new ArgumentException("Connection is not active", paramName);
        }

        [UnityEngine.HideInCallstack]
        internal static void ArgumentNonSerializableType(Type type)
        {
            throw new ArgumentException($"{type} is not blittable or {nameof(LayoutKind)} of {type} is not {LayoutKind.Sequential} or {type} is packed");
        }

        [UnityEngine.HideInCallstack]
        internal static void TooManyScriptsOnScene(string scriptName)
        {
            throw new Exception($"Too many {scriptName} scripts on a scene");
        }

        [UnityEngine.HideInCallstack]
        internal static void MustUseOnClient(string memberName)
        {
            throw new InvalidOperationException($"{memberName} must be used on a client");
        }

        [UnityEngine.HideInCallstack]
        internal static void ThrowIfNotClient(string memberName)
        {
            if (NetworkApplication.IsClient == false)
                MustUseOnClient(memberName);
        }

        [UnityEngine.HideInCallstack]
        internal static void MustUseOnServer(string memberName)
        {
            throw new InvalidOperationException($"{memberName} must be used on a server");
        }

        [UnityEngine.HideInCallstack]
        internal static void ThrowIfNotServer(string memberName)
        {
            if (NetworkApplication.IsServer == false)
                MustUseOnServer(memberName);
        }

        [UnityEngine.HideInCallstack]
        internal static void GameObjectNotIdentifiable(string name)
        {
            throw new NullReferenceException($"GameObject {name} has not {nameof(NetworkIdentity)} component");
        }

        [UnityEngine.HideInCallstack]
        internal static void GameObjectNotInitialized(string name)
        {
            throw new ArgumentException($"Object {name} is not initialized");
        }

        [UnityEngine.HideInCallstack]
        internal static void UnableChangePrefab(string paramName)
        {
            throw new ArgumentException("Unable to change prefabs", paramName);
        }

        [UnityEngine.HideInCallstack]
        internal static void ConnectionObjIncorrect(object obj)
        {
            throw new ArgumentException($"Room with {obj} does not contains this connection");
        }

        [UnityEngine.HideInCallstack]
        internal static void IncorrectSyncObjectData(Exception innerException)
        {
            throw new InvalidNetworkDataException($"Incorrect {nameof(SyncObject)} data", innerException);
        }

        [UnityEngine.HideInCallstack]
        internal static void InvalidSyncModeInDataHandler(SyncObject syncObj, SyncMode expectedMode)
        {
            throw new InvalidOperationException($"Data handler of {nameof(SyncObject)} with script owner {syncObj.Owner} must be called with mode {expectedMode}");
        }

        [UnityEngine.HideInCallstack]
        internal static void ShouldUseWithSyncMode(string memberName, SyncMode expectedMode)
        {
            throw new InvalidOperationException($"{memberName} must be used with {expectedMode}");
        }
    }
}