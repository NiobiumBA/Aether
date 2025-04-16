namespace Aether
{
    public static class StableHash
    {
        public static ushort GetHash(string str)
        {
            ushort sum1 = 0;
            ushort sum2 = 0;

            foreach (char c in str)
            {
                sum1 = (ushort)((sum1 + c) % 255);
                sum2 = (ushort)((sum2 + sum1) % 255);
            }

            return (ushort)((sum2 << 8) | sum1);
        }
    }
}
