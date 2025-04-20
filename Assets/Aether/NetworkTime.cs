using Aether.Messages;
using System.Collections;
using UnityEngine;

namespace Aether
{
    public class NetworkTime : NetworkBehaviour
    {
        [NetworkMessageName("Ping")]
        private struct PingMessage : INetworkMessage
        {
            public float clientTime;
        }

        [NetworkMessageName("Pong")]
        private struct PongMessage : INetworkMessage
        {
            public float clientTime;
            public float serverTime;
        }

        private static bool m_eventSubscribed;
        private static float m_timeDifference;
        private static float m_oneWayDelay;

        // TODO smoothing
        public static float TimeSinceServerStartup => NetworkApplication.IsServer ?
                                                      Time.realtimeSinceStartup :
                                                      Time.realtimeSinceStartup - m_timeDifference;

        public static float Ping => NetworkApplication.IsServer ? 0 : m_oneWayDelay;

        private static void PingMessageHandler(NetworkConnection connection, PingMessage pingMessage)
        {
            PongMessage pongMessage = new()
            {
                clientTime = pingMessage.clientTime,
                serverTime = TimeSinceServerStartup
            };

            NetworkDispatcher.SendMessageByConnection(connection, pongMessage);
        }

        private static void PongMessageCallback(PongMessage pongMessage)
        {
            float roundTripTime = TimeSinceServerStartup - pongMessage.clientTime;
            m_oneWayDelay = roundTripTime * 0.5f;

            m_timeDifference = Time.realtimeSinceStartup - (pongMessage.serverTime + m_oneWayDelay);
        }

        private static void RegisterHandlers()
        {
            if (NetworkApplication.IsClient)
            {
                NetworkApplication.ClientDispatcher.RegisterMessageCallback<PongMessage>(PongMessageCallback);
            }

            if (NetworkApplication.IsServer)
            {
                NetworkApplication.ServerDispatcher.RegisterMessageCallback<PingMessage>(PingMessageHandler);
            }

            m_eventSubscribed = true;
        }

        [SerializeField] private float m_pingMessageFrequency = 0.5f;

        private void Start()
        {
            if (m_eventSubscribed == false)
                RegisterHandlers();

            if (NetworkApplication.IsClientOnly)
                StartCoroutine(SendingPingMessages());
        }

        private IEnumerator SendingPingMessages()
        {
            while (true)
            {
                if (!isActiveAndEnabled || !NetworkApplication.IsClientOnly || !NetworkApplication.ClientConnected)
                {
                    yield return null;
                    continue;
                }

                PingMessage pingMessage = new()
                {
                    clientTime = TimeSinceServerStartup
                };

                NetworkApplication.ClientDispatcher.SendMessage(pingMessage);

                yield return new WaitForSeconds(m_pingMessageFrequency);
            }
        }
    }
}
