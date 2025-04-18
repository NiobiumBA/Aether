using Aether;
using Aether.SceneManagement;
using System.Linq;
using UnityEngine;

namespace Assets.Examples
{
    public class NetTransformInterests : NetworkBehaviour
    {
        [SerializeField] private NetworkTransform m_transform;

        private void Start()
        {
            if (NetworkApplication.IsServer && m_transform.Mode == Aether.Synchronization.SyncMode.ClientOwner)
                m_transform.OwnerClient = NetworkRoomManager.GetRoom(gameObject.scene).Clients.FirstOrDefault();
        }
    }
}