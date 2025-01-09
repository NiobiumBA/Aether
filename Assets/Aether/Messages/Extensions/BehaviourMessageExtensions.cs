namespace Aether.Messages.Extensions
{
    public static class BehaviourMessageExtensions
    {
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
