using Aether.Synchronization;
using System;
using System.Runtime.InteropServices;

namespace Aether
{
    internal static class ThrowHelper
    {
        internal static void RepeatedMessageRegister(string messageHandlerName)
        {
            throw new ArgumentException($"A callback of message with name {messageHandlerName} already registered");
        }

        internal static void RepeatedHandlerRegister(string handlerName)
        {
            throw new ArgumentException($"A handler with name {handlerName} already registered");
        }

        internal static void ArgumentInactiveConnection(string paramName)
        {
            throw new ArgumentException("Connection is not active", paramName);
        }

        internal static void ArgumentNonSerializableType(Type type)
        {
            throw new ArgumentException($"{type} is not blittable or {nameof(LayoutKind)} of {type} is not {LayoutKind.Sequential} or {type} is packed");
        }

        internal static void TooManyScriptsOnScene(string scriptName)
        {
            throw new Exception($"Too many {scriptName} scripts on a scene");
        }

        internal static void MustUseOnClient(string memberName)
        {
            throw new InvalidOperationException($"{memberName} must be used on a client");
        }

        internal static void ThrowIfNotClient(string memberName)
        {
            if (NetworkApplication.IsClient == false)
                MustUseOnClient(memberName);
        }

        internal static void MustUseOnServer(string memberName)
        {
            throw new InvalidOperationException($"{memberName} must be used on a server");
        }

        internal static void ThrowIfNotServer(string memberName)
        {
            if (NetworkApplication.IsServer == false)
                MustUseOnServer(memberName);
        }

        internal static void GameObjectNotIdentifiable(string name)
        {
            throw new NullReferenceException($"GameObject {name} has not {nameof(NetworkIdentity)} component");
        }

        internal static void GameObjectNotInitialized(string name)
        {
            throw new ArgumentException($"Object {name} is not initialized");
        }

        internal static void UnableChangePrefab(string paramName)
        {
            throw new ArgumentException("Unable to change prefabs", paramName);
        }

        internal static void IncorrectSyncObjectData()
        {
            throw new Exception($"Incorrect {nameof(SyncObject)} data");
        }

        internal static void InvalidSyncModeInDataHandler(SyncMode expectedMode)
        {
            throw new InvalidOperationException($"Data handler of {nameof(SyncObject)} must be called with mode {expectedMode}");
        }

        internal static void ShouldUseWithSyncMode(string memberName, SyncMode expectedMode)
        {
            throw new InvalidOperationException($"{memberName} must be used with {expectedMode}");
        }
    }
}