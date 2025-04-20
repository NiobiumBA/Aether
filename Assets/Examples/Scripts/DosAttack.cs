using Aether;
using Aether.Messages;
using UnityEngine;

namespace Examples
{
    public class DosAttack : NetworkBehaviour
    {
        [NetworkMessageName("DosAttack")]
        private struct AttackMessage : INetworkMessage
        {
            public int value;
        }

        [SerializeField] private int m_countMessages;

        private void Start()
        {
            RegisterMessageCallback<AttackMessage>(Callback);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                for (int i = 0; i < m_countMessages; i++)
                {
                    AttackMessage message = new()
                    {
                        value = Random.Range(0, int.MaxValue),
                    };

                    SendMessageToServer(message);
                }
            }
        }

        private void Callback(AttackMessage message)
        {
        }
    }
}