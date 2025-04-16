using Aether;
using Aether.Messages;
using UnityEngine;

public class MouseSpawnObject : NetworkBehaviour
{
    private struct SpawnMessage : INetworkMessage
    {
        public Ray ray;
    }

    [SerializeField] private NetworkIdentity m_spawnObject;

    private void Start()
    {
        RegisterMessageCallback<SpawnMessage>(Spawn);
    }

    protected override void ClientUpdate()
    {
        if (NetworkApplication.ClientConnected && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (gameObject.scene.GetPhysicsScene().Raycast(ray.origin, ray.direction) == false)
            {
                SpawnMessage message = new()
                {
                    ray = ray
                };

                SendMessageToServer(message);
            }
        }
    }

    private void Spawn(NetworkConnection conn, SpawnMessage message)
    {
        Vector3 pos = message.ray.GetPoint(5);

        NetworkGameObjects.Spawn(m_spawnObject, pos, Quaternion.identity, Identity);
    }
}
