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

        private static bool s_eventSubscribed;
        private static float s_timeDifferenceCurrent;
        private static float s_timeDifferenceTarget;
        private static float s_timeDifferenceVelocity;
        private static float s_oneWayDelay;

        public static float TimeSinceServerStartup => NetworkApplication.IsServer ?
                                                      Time.realtimeSinceStartup :
                                                      Time.realtimeSinceStartup - s_timeDifferenceCurrent;

        public static float Ping => NetworkApplication.IsServer ? 0 : s_oneWayDelay;

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

            if (roundTripTime < 0)
                return;
            
            s_oneWayDelay = roundTripTime * 0.5f;

            s_timeDifferenceTarget = Time.realtimeSinceStartup - (pongMessage.serverTime + s_oneWayDelay);
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

            s_eventSubscribed = true;
        }

        [SerializeField] private float m_pingMessageFrequency = 1f;
        [SerializeField] private float m_timeSmoothFactor = 1f;

        private void Start()
        {
            if (s_eventSubscribed == false)
                RegisterHandlers();

            if (NetworkApplication.IsClientOnly)
                StartCoroutine(SendingPingMessages());
        }

        protected internal override void ClientUpdate()
        {
            s_timeDifferenceCurrent = Mathf.SmoothDamp(s_timeDifferenceCurrent,
                                                       s_timeDifferenceTarget,
                                                       ref s_timeDifferenceVelocity,
                                                       m_timeSmoothFactor);
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
