using UnityEngine;

namespace Aether
{
    [RequireComponent(typeof(NetworkIdentity))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        public NetworkIdentity Identity { get; internal set; }
        public byte ComponentId { get; internal set; }

        protected internal virtual void OnConnect(NetworkConnection connection) { }

        protected internal virtual void OnDisconnect(NetworkConnection connection) { }

        /// <summary>
        /// Called on Start if NetworkApplication is client or on client dispatcher create.
        /// </summary>
        protected internal virtual void ClientStart() { }

        /// <summary>
        /// Called on Update if NetworkApplication is client.
        /// </summary>
        protected internal virtual void ClientUpdate() { }

        /// <summary>
        /// Called on FixedUpdate if NetworkApplication is client.
        /// </summary>
        protected internal virtual void ClientFixedUpdate() { }

        /// <summary>
        /// Called on Start if NetworkApplication is server or on server dispatcher create.
        /// </summary>
        protected internal virtual void ServerStart() { }

        /// <summary>
        /// Called on Update if NetworkApplication is server.
        /// </summary>
        protected internal virtual void ServerUpdate() { }
    }
}