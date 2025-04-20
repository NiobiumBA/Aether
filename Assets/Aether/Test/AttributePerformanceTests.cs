using Aether.Messages;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Aether.Test
{
    public class AttributePerformanceTests
    {
        [NetworkMessageName("MyMessage1")]
        public struct MyMessage1 : INetworkMessage
        {
        }

        [NetworkMessageName("MyMessage2")]
        public struct MyMessage2 : INetworkMessage
        {
        }

        [NetworkMessageName("MyMessage3")]
        public struct MyMessage3 : INetworkMessage
        {
        }

        [NetworkMessageName("MyMessage4")]
        public struct MyMessage4 : INetworkMessage
        {
        }

        private const int c_getAttributeCount = 100_000;

        private static class AttributeCache<TMessage>
            where TMessage : INetworkMessage
        {
            public static NetworkMessageNameAttribute attribute = null;
        }

        private static NetworkMessageNameAttribute GetAttribute<TMessage>()
            where TMessage : INetworkMessage
        {
            return typeof(TMessage).GetCustomAttribute<NetworkMessageNameAttribute>();
        }

        private static NetworkMessageNameAttribute GetAttributeCached<TMessage>()
            where TMessage : INetworkMessage
        {
            if (AttributeCache<TMessage>.attribute == null)
            {
                NetworkMessageNameAttribute attribute = typeof(TMessage).GetCustomAttribute<NetworkMessageNameAttribute>();
                AttributeCache<TMessage>.attribute = attribute;
                return attribute;
            }

            return AttributeCache<TMessage>.attribute;
        }

        [Test]
        public void GetAttributeTest()
        {
            List<string> list = new();

            for (int i = 0; i < c_getAttributeCount; i++)
            {
                string name;

                name = GetAttribute<MyMessage1>().CustomName;
                list.Add(name);
                name = GetAttribute<MyMessage2>().CustomName;
                list.Add(name);
                name = GetAttribute<MyMessage3>().CustomName;
                list.Add(name);
                name = GetAttribute<MyMessage4>().CustomName;
                list.Add(name);
            }
        }

        [Test]
        public void GetAttributeCacheTest()
        {
            List<string> list = new();

            for (int i = 0; i < c_getAttributeCount; i++)
            {
                string name;

                name = GetAttributeCached<MyMessage1>().CustomName;
                list.Add(name);
                name = GetAttributeCached<MyMessage2>().CustomName;
                list.Add(name);
                name = GetAttributeCached<MyMessage3>().CustomName;
                list.Add(name);
                name = GetAttributeCached<MyMessage4>().CustomName;
                list.Add(name);
            }
        }
    }
}
