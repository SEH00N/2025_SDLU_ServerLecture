using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CSGameServer
{
    public class GameServer
    {
        private Socket listenSocket = null;

        private ConcurrentDictionary<string, SessionHandle> sessions = null;

        public event Action<SessionHandle> OnSessionConnectedEvent = null;
        public event Action<SessionHandle> OnSessionDisconnectedEvent = null;

        public GameServer()
        {
            sessions = new ConcurrentDictionary<string, SessionHandle>();
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

            Session session = new Session(acceptArgs.AcceptSocket, sessionID);
            session.OnSessionClosedEvent += () => HandleSessionClosed(sessionID);
            session.Open();

            SessionHandle sessionHandle = new SessionHandle(session);
            sessions.TryAdd(sessionID, sessionHandle);

            OnSessionConnectedEvent?.Invoke(sessionHandle);

            AcceptAsync(acceptArgs);
        }

        public bool TryGetSession(string sessionID, out SessionHandle sessionHandle)
        {
            return sessions.TryGetValue(sessionID, out sessionHandle);
        }

        public void Send(Session session, Packet packet)
        {
            if (PacketManager.TrySerializePacket(packet, out ArraySegment<byte> serializedBuffer) == false)
                return;

            session.SendAsync(serializedBuffer);
        }

        public void SendAll(Packet packet, Func<SessionHandle, bool> filter = null)
        {
            if (PacketManager.TrySerializePacket(packet, out ArraySegment<byte> serializedBuffer) == false)
                return;

            IEnumerable<SessionHandle> sessionHandles = sessions.Values;
            foreach (SessionHandle sessionHandle in sessionHandles)
            {
                if (filter != null && filter(sessionHandle) == false)
                    continue;

                sessionHandle.Session.SendAsync(serializedBuffer);
            }
        }

        private void HandleSessionClosed(string sessionID)
        {
            if (sessions.TryRemove(sessionID, out SessionHandle sessionHandle) == false)
                return;

            OnSessionDisconnectedEvent?.Invoke(sessionHandle);
        }
    }
}
