using System;
using System.Reflection;
using UnityEngine;

namespace Aether.Messages
{
    public class MessageHandling
    {
        private static class MessageCache<TMessage>
            where TMessage : INetworkMessage
        {
            public static string name;
        }

        public static string GetMessageHandlerName<TMessage>()
            where TMessage : unmanaged, INetworkMessage
        {
            if (MessageCache<TMessage>.name == null)
            {
                Type type = typeof(TMessage);
                NetworkMessageNameAttribute attribute = type.GetCustomAttribute<NetworkMessageNameAttribute>();
                
                string name = attribute == null ? type.FullName : attribute.CustomName;
                MessageCache<TMessage>.name = name;
                return name;
            }

            return MessageCache<TMessage>.name;
        }

        public unsafe static int GetMessageSize<TMessage>()
            where TMessage : unmanaged, INetworkMessage
        {
            return sizeof(TMessage);
        }

        public static NetworkDataHandler GetMessageHandler<TMessage>(NetworkMessageCallback<TMessage> messageCallback)
            where TMessage : unmanaged, INetworkMessage
        {
            ThrowHelper.ThrowIfNull(messageCallback, nameof(messageCallback));

            return (conn, reader) =>
            {
                // Using try..catch to avoid a DOS attacks
                TMessage message = default;

                try
                {
                    message = reader.ReadMessage<TMessage>();
                }
                catch (Exception exception)
                {
                    throw new InvalidNetworkDataException($"Failed to read message of type {typeof(TMessage)}.", exception);
                }

                try
                {
                    messageCallback(conn, message);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Caught exception in handler with name: {GetMessageHandlerName<TMessage>()}\n" +
                                   $"{exception}");
                    conn.Disconnect();
                }
            };
        }
    }
}