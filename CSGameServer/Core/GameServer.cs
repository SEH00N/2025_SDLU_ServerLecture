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

        private ConcurrentDictionary<string, ClientSessionHandle> sessionHandles = null;

        public event Action<ClientSession> OnSessionConnectedEvent = null;
        public event Action<ClientSession> OnSessionDisconnectedEvent = null;

        public GameServer(World world)
        {
            this.world = world;
            sessionHandles = new ConcurrentDictionary<string, ClientSessionHandle>();
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

            ClientSessionHandle sessionHandle = new ClientSessionHandle(this, session, world);
            sessionHandles.TryAdd(sessionID, sessionHandle);

            OnSessionConnectedEvent?.Invoke(session);

            AcceptAsync(acceptArgs);
        }

        public IEnumerable<ClientSessionHandle> GetAllSessionHandles() => sessionHandles.Values;
        public bool TryGetSessionHandle(string sessionID, out ClientSessionHandle sessionHandle)
        {
            return sessionHandles.TryGetValue(sessionID, out sessionHandle);
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

            IEnumerable<ClientSessionHandle> clientSessionHandles = sessionHandles.Values;
            foreach (ClientSessionHandle sessionHandle in clientSessionHandles)
            {
                if (filter != null && filter(sessionHandle.Session) == false)
                    continue;

                sessionHandle.Session.SendAsync(serializedBuffer);
            }
        }

        private void HandleSessionClosed(string sessionID)
        {
            if (sessionHandles.TryRemove(sessionID, out ClientSessionHandle sessionHandle) == false)
                return;

            OnSessionDisconnectedEvent?.Invoke(sessionHandle.Session);
        }
    }
}
