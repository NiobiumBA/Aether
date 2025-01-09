using Aether.Connections;
using Aether.Messages.Extensions;
using System;
using System.Collections.Generic;

namespace Aether.Synchronization
{
    public enum SyncMode : byte
    {
        ServerOwner, ClientOwner
    }

    /// <summary>
    /// Should call constructor in Awake and don't send changes in it and
    /// dispose it in method OnDestroy.
    /// </summary>
    public abstract class SyncObject : IDisposable
    {
        /// <summary>
        /// Synchronization provides by NetworkApplication.
        /// </summary>
        public static class EventSystem
        {
            private static NetworkClientDispatcher s_clientDispatcher = null;
            private static NetworkServerDispatcher s_serverDispatcher = null;

            public static bool EnabledOnClient => s_clientDispatcher != null;
            public static bool EnabledOnServer => s_serverDispatcher != null;

            public static void EnableOnClient()
            {
                ThrowHelper.ThrowIfNotClient(nameof(EnableOnClient));

                if (EnabledOnClient)
                    DisableOnClient();

                s_clientDispatcher = NetworkApplication.ClientDispatcher;
                s_clientDispatcher.RegisterDataHandler(c_dataHandlerName, DataHandlerOnClient);
            }

            public static void DisableOnClient()
            {
                ThrowHelper.ThrowIfNotClient(nameof(DisableOnClient));

                if (EnabledOnClient == false)
                    return;

                s_clientDispatcher.RemoveHandler(c_dataHandlerName);
                s_clientDispatcher = null;
            }

            public static void EnableOnServer()
            {
                ThrowHelper.ThrowIfNotServer(nameof(EnableOnServer));

                if (EnabledOnServer)
                    DisableOnServer();

                s_serverDispatcher = NetworkApplication.ServerDispatcher;
                s_serverDispatcher.RegisterDataHandler(c_dataHandlerName, DataHandlerOnServer);
            }

            public static void DisableOnServer()
            {
                ThrowHelper.ThrowIfNotServer(nameof(DisableOnServer));

                if (EnabledOnServer == false)
                    return;

                s_serverDispatcher.RemoveHandler(c_dataHandlerName);
                s_serverDispatcher = null;
            }
        }



        private const string c_dataHandlerName = nameof(SyncObject);

        private static readonly Dictionary<NetworkBehaviour, List<SyncObject>> s_syncObjects = new();


        public static IEnumerable<SyncObject> AllSyncObjects
        {
            get
            {
                foreach (List<SyncObject> list in s_syncObjects.Values)
                {
                    foreach (SyncObject syncObject in list)
                    {
                        yield return syncObject;
                    }
                }
            }
        }


        public static bool TryGetSyncObjects(NetworkBehaviour owner, out IReadOnlyList<SyncObject> objects)
        {
            bool result = s_syncObjects.TryGetValue(owner, out List<SyncObject> list);
            objects = list;
            return result;
        }


        private static void DataHandlerOnClient(NetworkReader reader)
        {
            SyncObject syncObj = null;

            try
            {
                syncObj = ReadSyncObject(reader);
            }
            catch (Exception)
            {
                ThrowHelper.IncorrectSyncObjectData();
            }

            syncObj.OnChangeReceived(reader, NetworkApplication.ClientDispatcher.Connection);
            syncObj.Changed?.Invoke();
        }

        private static void DataHandlerOnServer(NetworkConnection senderConn, NetworkReader reader)
        {
            ArraySegment<byte> remainingData = reader.ToArraySegment();

            int startPosition = reader.Position;

            SyncObject syncObj = null;

            try
            {
                syncObj = ReadSyncObject(reader);
            }
            catch (Exception)
            {
                ThrowHelper.IncorrectSyncObjectData();
            }

            if (syncObj.Mode != SyncMode.ClientOwner)
            {
                ThrowHelper.InvalidSyncModeInDataHandler(SyncMode.ClientOwner);
            }

            // Check if the sender is the owner
            if (syncObj.OwnerConnections.Contains(senderConn as ConnectionToClient) == false)
            {
                throw new Exception($"The connection is not owner of this {nameof(SyncObject)}.");
            }

            syncObj.OnChangeReceived(reader, senderConn);

            int bytesRead = reader.Position - startPosition;

            ArraySegment<byte> data = remainingData.Slice(0, bytesRead);

            foreach (NetworkConnection conn in NetworkApplication.ServerDispatcher.Connections)
            {
                if (senderConn != conn)
                    NetworkDispatcher.SendByConnection(conn, c_dataHandlerName, data);
            }

            syncObj.Changed?.Invoke();
        }

        private static SyncObject ReadSyncObject(NetworkReader reader)
        {
            NetworkBehaviour component = reader.ReadNetworkBehaviour();
            byte syncObjectId = reader.ReadByte();

            return s_syncObjects[component][syncObjectId];
        }


        public event Action Changed;

        private readonly SyncMode m_mode;
        private readonly NetworkBehaviour m_owner;
        private readonly byte m_id;

        private List<ConnectionToClient> m_ownerConnections;

        public SyncMode Mode => m_mode;

        public NetworkBehaviour Owner => m_owner;

        public List<ConnectionToClient> OwnerConnections
        {
            get
            {
                ThrowHelper.ThrowIfNotServer(nameof(OwnerConnections));

                if (Mode != SyncMode.ClientOwner)
                    ThrowHelper.ShouldUseWithSyncMode(nameof(OwnerConnections), SyncMode.ClientOwner);

                return m_ownerConnections;
            }
            protected set
            {
                ThrowHelper.ThrowIfNotServer(nameof(OwnerConnections));

                if (Mode != SyncMode.ClientOwner)
                    ThrowHelper.ShouldUseWithSyncMode(nameof(OwnerConnections), SyncMode.ClientOwner);

                m_ownerConnections = value;
            }
        }

        protected SyncObject(NetworkBehaviour owner, SyncMode mode)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            m_mode = mode;
            m_owner = owner;

            m_id = RegisterSyncObject();

            if (mode == SyncMode.ClientOwner)
                m_ownerConnections = new List<ConnectionToClient>();
        }

        public void SendInitData(NetworkConnection connection)
        {
            ArraySegment<byte> data = GetDataWithSyncObjectInfo(GetInitData());

            NetworkDispatcher.SendByConnection(connection, c_dataHandlerName, data);
        }

        public virtual void Dispose()
        {
            List<SyncObject> syncObjectsWithSameOwner = s_syncObjects[m_owner];
            syncObjectsWithSameOwner.Remove(this);

            if (syncObjectsWithSameOwner.Count == 0)
                s_syncObjects.Remove(m_owner);
        }

        protected abstract ArraySegment<byte> GetInitData();

        protected abstract void OnChangeReceived(NetworkReader reader, NetworkConnection connection);

        protected void SendChanges(ArraySegment<byte> data)
        {
            ArraySegment<byte> resultData = GetDataWithSyncObjectInfo(data);

            if (Mode == SyncMode.ClientOwner)
            {
                if (NetworkApplication.IsClient == false)
                    throw new InvalidOperationException($"Attempt to send changes with mode {Mode}," +
                        $" but {nameof(NetworkApplication)} is not client");

                if (NetworkApplication.ClientConnected)
                {
                    if (NetworkApplication.IsServer)
                        NetworkApplication.ServerDispatcher.SendAllRemote(c_dataHandlerName, resultData);
                    else
                        NetworkApplication.ClientDispatcher.Send(c_dataHandlerName, resultData);
                }
            }
            else if (Mode == SyncMode.ServerOwner)
            {
                if (NetworkApplication.IsServer == false)
                    throw new InvalidOperationException($"Attempt to send changes with mode {Mode}," +
                        $" but {nameof(NetworkApplication)} is not server");

                NetworkApplication.ServerDispatcher.SendAllRemote(c_dataHandlerName, resultData);
            }

            Changed?.Invoke();
        }

        protected void SendChangesByConnection(NetworkConnection connection, ArraySegment<byte> data)
        {
            ArraySegment<byte> resultData = GetDataWithSyncObjectInfo(data);

            connection.Send(resultData);
        }

        private ArraySegment<byte> GetDataWithSyncObjectInfo(ArraySegment<byte> data)
        {
            NetworkWriter writer = new();
            writer.WriteNetworkBehaviour(m_owner);
            writer.WriteByte(m_id);
            writer.WriteBytes(data);

            return writer.ToArraySegment();
        }

        private byte RegisterSyncObject()
        {
            if (s_syncObjects.TryGetValue(m_owner, out List<SyncObject> behaviourSyncObjects))
            {
                behaviourSyncObjects.Add(this);
                int result = behaviourSyncObjects.Count - 1;

                if (result > byte.MaxValue)
                    throw new InvalidOperationException($"Exceeded the number of {nameof(SyncObject)} for one {nameof(NetworkBehaviour)}.");

                return (byte)result;
            }

            s_syncObjects[m_owner] = new List<SyncObject>()
            {
                this
            };
            return 0;
        }
    }
}
