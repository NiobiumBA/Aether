using Aether;
using Aether.Transports;
using UnityEngine;
using UnityEngine.UI;

public class TransportErrorsText : MonoBehaviour
{
    [SerializeField] private Text m_errorText;

    private void OnEnable()
    {
        NetworkApplication.OnTransportChange += OnTransportChange;

        if (NetworkApplication.ActiveTransport != null)
            SubscribeTransportEvents();
    }

    private void OnDisable()
    {
        NetworkApplication.OnTransportChange -= OnTransportChange;

        if (NetworkApplication.ActiveTransport != null)
            UnsubscribeTransportEvents();
    }

    private void OnTransportChange(NetworkTransport transport)
    {
        NetworkTransport lastTransport = NetworkApplication.ActiveTransport;

        if (lastTransport != null)
            lastTransport.OnTransportError -= OnTransportError;

        if (transport != null)
            transport.OnTransportError += OnTransportError;
    }

    private void SubscribeTransportEvents()
    {
        NetworkApplication.ActiveTransport.OnTransportError += OnTransportError;
    }

    private void UnsubscribeTransportEvents()
    {
        NetworkApplication.ActiveTransport.OnTransportError -= OnTransportError;
    }

    private void OnTransportError(NetworkTransport.TransportError error)
    {
        TcpTransport.ExceptionError exceptionError = error as TcpTransport.ExceptionError;

        m_errorText.text = $"Error type: {error.GetType()}\n{exceptionError.Exception}";
    }
}
