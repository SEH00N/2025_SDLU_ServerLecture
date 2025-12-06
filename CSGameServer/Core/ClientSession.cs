using System.Net.Sockets;

namespace CSGameServer
{
    public class ClientSession : Session
    {
        private string sessionID = null;
        public string SessionID => sessionID;

        public ClientSession(Socket connectedSocket, string sessionID) : base(connectedSocket)
        {
            this.sessionID = sessionID;
        }

    }
}