using Aether;
using Aether.Messages;
using UnityEngine;

public class MouseHolder : NetworkBehaviour
{
    private struct DragMessage : INetworkMessage
    {
        public Vector3 position;
    }

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

            DragMessage message = new()
            {
                position = position
            };

            SendMessageToServer(message);
        }
    }

    private void OnDragMessageReceive(DragMessage message)
    {
        transform.position = message.position;
    }
}
