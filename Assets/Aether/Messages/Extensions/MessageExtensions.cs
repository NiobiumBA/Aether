using UnityEngine;

namespace Aether.Messages.Extensions
{
    public static class MessageExtensions
    {
        public static void WriteGameObject(this NetworkWriter writer, GameObject gameObject)
        {
            writer.WriteMessage<GameObjectMessage>(new GameObjectMessage(gameObject));
        }

        public static GameObject ReadGameObject(this NetworkReader reader)
        {
            return reader.ReadMessage<GameObjectMessage>().Object;
        }

        public static void WriteNetworkBehaviour(this NetworkWriter writer, NetworkBehaviour behaviour)
        {
            writer.WriteMessage<BehaviourMessage>(new BehaviourMessage(behaviour));
        }

        public static NetworkBehaviour ReadNetworkBehaviour(this NetworkReader reader)
        {
            return reader.ReadMessage<BehaviourMessage>().Component;
        }

        public static T ReadNetworkBehaviour<T>(this NetworkReader reader)
            where T : NetworkBehaviour
        {
            return reader.ReadNetworkBehaviour() as T;
        }
    }
}
