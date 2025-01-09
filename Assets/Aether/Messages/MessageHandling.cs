using System;
using UnityEngine;

namespace Aether.Messages
{
    public class MessageHandling
    {
        public static string GetMessageHandlerName<TMessage>()
            where TMessage : unmanaged, INetworkMessage
        {
            return typeof(TMessage).FullName;
        }

        public static NetworkDataHandler GetMessageHandler<TMessage>(NetworkMessageCallback<TMessage> messageCallback)
            where TMessage : unmanaged, INetworkMessage
        {
            if (messageCallback == null)
                throw new ArgumentNullException(nameof(messageCallback));

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
                    throw new Exception($"Failed to read message of type {typeof(TMessage)}.", exception);
                }

                try
                {
                    messageCallback(conn, message);
                }
                catch (Exception exception)
                {
                    conn.Disconnect();
                    Debug.LogError($"{exception}\nin message handler with name: {GetMessageHandlerName<TMessage>()}");
                }
            };
        }
    }
}