using Aether;
using Aether.Connections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnDisconnect : NetworkBehaviour
{
    [SerializeField] private int m_sceneId;

    protected override void OnDisconnect(NetworkConnection conn)
    {
        if (conn is ConnectionToServer and not LocalConnectionToServer)
        {
            OnClientRemoteForcedDisconnect();
        }
    }

    private void OnClientRemoteForcedDisconnect()
    {
        SceneManager.LoadScene(m_sceneId);
    }
}
