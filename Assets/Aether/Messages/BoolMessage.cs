namespace Aether.Messages
{
    /// <summary>
    /// Encapsulate bool value in byte
    /// because bool is not blittable, but byte is blittable.
    /// Use this type instead of bool in your network messages.
    /// </summary>
    public readonly struct BoolMessage : INetworkMessage
    {
        private readonly byte m_value;

        public BoolMessage(bool value)
        {
            m_value = value ? (byte)1 : (byte)0;
        }

        public static implicit operator BoolMessage(bool value)
        {
            return new BoolMessage(value);
        }

        public static implicit operator bool(BoolMessage message)
        {
            return message.m_value != 0;
        }
    }
}
