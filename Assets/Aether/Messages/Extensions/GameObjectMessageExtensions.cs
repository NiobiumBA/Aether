using UnityEngine;

namespace Aether.Messages.Extensions
{
    public static class GameObjectMessageExtensions
    {
        public static void WriteGameObject(this NetworkWriter writer, GameObject gameObject)
        {
            writer.WriteMessage<GameObjectMessage>(new GameObjectMessage(gameObject));
        }

        public static GameObject ReadGameObject(this NetworkReader reader)
        {
            return reader.ReadMessage<GameObjectMessage>().Object;
        }
    }
}
