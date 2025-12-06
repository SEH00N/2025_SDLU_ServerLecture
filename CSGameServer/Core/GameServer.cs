using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CSGameServer
{
    public class GameServer
    {
        private World world = null;
        private Socket listenSocket = null;

        private ConcurrentDictionary<string, ClientSessionHandle> sessions = null;

        public event Action<ClientSession> OnSessionConnectedEvent = null;
        public event Action<ClientSession> OnSessionDisconnectedEvent = null;

        public GameServer(World world)
        {
            this.world = world;
            sessions = new ConcurrentDictionary<string, ClientSessionHandle>();
        }

        public void StartServer(int port, int backlog = 10)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));

            listenSocket.Listen(backlog);

            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += HandleAccepted;

            AcceptAsync(acceptArgs);
        }

        private void AcceptAsync(SocketAsyncEventArgs acceptArgs)
        {
            acceptArgs.AcceptSocket = null;

            bool isPending = listenSocket.AcceptAsync(acceptArgs);
            if (isPending == false)
                HandleAccepted(null, acceptArgs);
        }

        private void HandleAccepted(object sender, SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs.SocketError != SocketError.Success || acceptArgs.AcceptSocket == null)
            {
                AcceptAsync(acceptArgs);
                return;
            }

            string sessionID = Guid.NewGuid().ToString();

            ClientSession session = new ClientSession(acceptArgs.AcceptSocket, sessionID);
            session.OnSessionClosedEvent += () => HandleSessionClosed(sessionID);
            session.Open();

            ClientSessionHandle sessionHandle = new ClientSessionHandle(session, world);
            sessions.TryAdd(sessionID, sessionHandle);

            OnSessionConnectedEvent?.Invoke(session);

            AcceptAsync(acceptArgs);
        }

        public bool TryGetSession(string sessionID, out ClientSessionHandle sessionHandle)
        {
            return sessions.TryGetValue(sessionID, out sessionHandle);
        }

        public void Send(ClientSession session, Packet packet)
        {
            if (PacketManager.TrySerializePacket(packet, out ArraySegment<byte> serializedBuffer) == false)
                return;

            session.SendAsync(serializedBuffer);
        }

        public void SendAll(Packet packet, Func<ClientSession, bool> filter = null)
        {
            if (PacketManager.TrySerializePacket(packet, out ArraySegment<byte> serializedBuffer) == false)
                return;

            IEnumerable<ClientSessionHandle> sessionHandles = sessions.Values;
            foreach (ClientSessionHandle sessionHandle in sessionHandles)
            {
                if (filter != null && filter(sessionHandle.Session) == false)
                    continue;

                sessionHandle.Session.SendAsync(serializedBuffer);
            }
        }

        private void HandleSessionClosed(string sessionID)
        {
            if (sessions.TryRemove(sessionID, out ClientSessionHandle sessionHandle) == false)
                return;

            OnSessionDisconnectedEvent?.Invoke(sessionHandle.Session);
        }
    }
}
