using Aether;
using Aether.Connections;
using Aether.Synchronization;
using UnityEngine;

public class ColorChanger : NetworkBehaviour
{
    [SerializeField] private Color[] m_colors;

    private Material m_material;

    private SyncValue<int> m_colorId;

    protected override void OnConnect(NetworkConnection connection)
    {
        if (connection is ConnectionToClient connectionToClient)
        {
            m_colorId.OwnerConnections.Add(connectionToClient);
        }
    }

    private void Awake()
    {
        m_colorId = new SyncValue<int>(this, SyncMode.ClientOwner, ColorIdSetter);

        m_material = GetComponent<MeshRenderer>().material;

        ColorIdSetter(m_colorId);
    }

    protected override void ServerStart()
    {
        foreach (ConnectionToClient conn in NetworkApplication.ServerDispatcher.Connections)
        {
            m_colorId.OwnerConnections.Add(conn);
        }
    }

    private void OnDestroy()
    {
        m_colorId?.Dispose();
    }

    private void OnMouseDown()
    {
        m_colorId.Value = (m_colorId + 1) % m_colors.Length;
    }

    private int ColorIdSetter(int value)
    {
        m_material.color = m_colors[value];

        return value;
    }
}
