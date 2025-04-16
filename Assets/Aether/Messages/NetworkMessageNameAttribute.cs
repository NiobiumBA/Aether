using System;

namespace Aether.Messages
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class NetworkMessageNameAttribute : Attribute
    {
        private readonly string m_customName;

        public string CustomName => m_customName;

        public NetworkMessageNameAttribute(string customName)
        {
            m_customName = customName;
        }
    }
}
