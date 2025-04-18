using Aether;
using Aether.Messages;
using Aether.Synchronization;
using UnityEngine;

public class MouseHolder : NetworkBehaviour
{
    [NetworkMessageName("DragMouse")]
    private struct DragMessage : INetworkMessage
    {
        public Vector3 position;
    }

    [SerializeField] private NetworkTransform m_netTransform;

    private void Start()
    {
        RegisterMessageCallback<DragMessage>(OnDragMessageReceive);
    }

    private void OnMouseDrag()
    {
        if (NetworkApplication.IsClientOnly)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Vector3 position = ray.GetPoint(5);

            if (m_netTransform.Mode == SyncMode.ClientOwner)
            {
                transform.position = position;
            }
            else
            {
                DragMessage message = new()
                {
                    position = position
                };

                SendMessageToServer(message);
            }
        }
    }

    private void OnDragMessageReceive(DragMessage message)
    {
        transform.position = message.position;
    }
}
