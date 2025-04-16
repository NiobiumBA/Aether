namespace Aether
{
    public static class StableHash
    {
        public static ushort GetHash16(string str)
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

        public static uint GetHash32(string str)
        {
            uint sum1 = 0;
            uint sum2 = 0;

            foreach (char c in str)
            {
                sum1 = (sum1 + c) % 65535;
                sum2 = (sum2 + sum1) % 65535;
            }

            return (sum2 << 16) | sum1;
        }
    }
}
