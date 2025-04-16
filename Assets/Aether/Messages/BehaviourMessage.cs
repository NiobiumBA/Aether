namespace Aether.Messages
{
    public readonly struct BehaviourMessage : INetworkMessage
    {
        private readonly GameObjectMessage m_gameObjectMessage;
        private readonly byte m_componentId;

        public readonly NetworkBehaviour Component
        {
            get
            {
                NetworkIdentity identity = m_gameObjectMessage.Identity;

                if (identity == null)
                    return null;

                if (m_componentId >= identity.Components.Count)
                    throw new InvalidNetworkDataException($"There is no component with id: {m_componentId}");

                return identity.Components[m_componentId];
            }
        }

        public BehaviourMessage(NetworkBehaviour component)
        {
            if (component == null)
            {
                m_gameObjectMessage = new GameObjectMessage((NetworkIdentity)null);
                m_componentId = 0;
                return;
            }

            if (component.Identity == null)
                ThrowHelper.GameObjectNotInitialized(component.name);

            if (component.Identity.InitState == NetworkIdentity.InitializationState.None)
                ThrowHelper.GameObjectNotInitialized(component.name);

            m_gameObjectMessage = new GameObjectMessage(component.gameObject);
            m_componentId = component.ComponentId;
        }
    }
}
