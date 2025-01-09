using Aether;
using Aether.Synchronization;
using Aether.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TcpConnector : MonoBehaviour
{
    [SerializeField] private InputField m_addressField;
    [SerializeField] private Toggle m_startsServerToggle;
    [SerializeField] private int m_sceneId;

    private TcpTransport m_transport;

    public void Connect()
    {
        m_transport = new TcpTransport();

        m_transport.OnClientConnect += LoadScene;

        bool isServer = m_startsServerToggle.isOn;

        NetworkApplication.ActiveTransport = m_transport;

        NetworkApplication.CreateClientDispatcher();

        SyncObject.EventSystem.EnableOnClient();

        if (isServer)
        {
            NetworkApplication.CreateServerDispatcher();

            SyncObject.EventSystem.EnableOnServer();

            NetworkApplication.CreateLocalConnection();

            m_transport.StartServer(m_addressField.text);

            LoadScene();
        }
        else
            m_transport.ClientConnect(m_addressField.text);

    }

    private void LoadScene()
    {
        SceneManager.LoadScene(m_sceneId);
    }
}
