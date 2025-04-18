using Aether;
using Aether.Connections;
using Aether.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

public class RoomsDistributor : NetworkBehaviour
{
    [SerializeField] private int m_roomBuildId;

    private Dictionary<ConnectionToClient, NetworkRoom> m_rooms = new();

    private void OnRoomLoad(NetworkRoom room, ConnectionToClient conn)
    {
        if (conn.IsActive == false)
            return;

        room.AddClient(conn);
        m_rooms.Add(conn, room);
    }

    protected override void OnConnect(NetworkConnection conn)
    {
        if (conn is not ConnectionToClient)
            return;

        NetworkRoomManager.LoadRoomAsync(m_roomBuildId,
            room => OnRoomLoad(room, conn as ConnectionToClient));
    }

    protected override void OnDisconnect(NetworkConnection connection)
    {
        if (connection is not ConnectionToClient)
            return;

        NetworkRoom room = m_rooms[connection as ConnectionToClient];
        m_rooms.Remove(connection as ConnectionToClient);

        NetworkRoomManager.UnloadRoomAsync(room);
    }
}
