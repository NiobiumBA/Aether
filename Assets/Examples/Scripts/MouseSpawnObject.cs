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

    private void Update()
    {
        if (NetworkApplication.IsClient && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            SpawnMessage spawnMessage = new()
            {
                ray = ray
            };

            if (Physics.Raycast(ray) == false) // Does not spawn if click on other object
                NetworkApplication.ClientDispatcher.SendMessage(spawnMessage);
        }
    }

    private void Spawn(NetworkConnection conn, SpawnMessage message)
    {
        Vector3 pos = message.ray.GetPoint(5);

        NetworkGameObjectInteractions.Spawn(m_spawnObject, pos, Quaternion.identity, transform);
    }
}
