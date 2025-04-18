using Aether;
using Aether.SceneManagement;
using Aether.Synchronization;
using Aether.Transports;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TcpConnector : MonoBehaviour
{
    [SerializeField] private InputField m_addressField;
    [SerializeField] private int m_serverMainRoomBuildId;
    [SerializeField] private int m_connectionRoomBuildId;
    [SerializeField] private int m_mainMenuSceneBuildId;

    public void StartServer()
    {
        TcpTransport transport = new();

        NetworkApplication.ActiveTransport = transport;

        NetworkApplication.CreateServerDispatcher();

        SyncObject.EventSystem.EnableOnServer();

        transport.StartServer(m_addressField.text);

        NetworkRoomManager.LoadRoomAsync(m_serverMainRoomBuildId, LoadSceneMode.Single);
    }

    public void Connect()
    {
        TcpTransport transport = new();

        NetworkApplication.ActiveTransport = transport;

        NetworkApplication.CreateClientDispatcher();

        SyncObject.EventSystem.EnableOnClient();

        NetworkRoomManager.LoadRoomAsync(m_connectionRoomBuildId, LoadSceneMode.Single,
            room => OnConnectionRoomLoaded(room, transport));
    }

    private void OnConnectionRoomLoaded(NetworkRoom room, NetworkTransport transport)
    {
        transport.OnTransportError += OnTransportError;

        transport.ClientConnect(m_addressField.text);
    }

    private void OnTransportError(NetworkTransport.TransportError error)
    {
        SceneManager.LoadScene(m_mainMenuSceneBuildId);
        Debug.LogError("Error in transport on client");
    }
}
