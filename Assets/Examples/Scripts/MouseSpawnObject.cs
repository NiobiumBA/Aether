using Aether;
using Aether.Messages;
using UnityEngine;

public class MouseSpawnObject : NetworkBehaviour
{
    private struct SpawnMessage : INetworkMessage
    {
        public Ray ray;
    }

    [SerializeField] private GameObject m_spawnObject;

    protected override void ServerStart()
    {
        NetworkApplication.ServerDispatcher.RegisterMessageCallback<SpawnMessage>(Spawn);
    }

    private void OnDestroy()
    {
        if (NetworkApplication.IsServer)
            NetworkApplication.ServerDispatcher.RemoveMessageCallback<SpawnMessage>();
    }

    protected override void ClientUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray) == false)
            {
                SpawnMessage message = new()
                {
                    ray = ray
                };

                NetworkApplication.ClientDispatcher.SendMessage(message);
            }
        }
    }

    private void Spawn(NetworkConnection conn, SpawnMessage message)
    {
        Vector3 pos = message.ray.GetPoint(5);

        NetworkGameObjectInteractions.Spawn(m_spawnObject, pos, Quaternion.identity, transform);
    }
}
