using System;

namespace CSGameServer
{
    public class ClientSessionHandle
    {
        public GameServer GameServer { get; set; }
        public ClientSession Session { get; set; }
        public World World { get; set; }
        public PlayerEntity PlayerEntity { get; set; }
        
        public ClientSessionHandle(GameServer gameServer, ClientSession session, World world)
        {
            GameServer = gameServer;
            Session = session;
            World = world;
            Session.OnSessionClosedEvent += HandleSessionClosed;
            Session.OnPacketReceivedEvent += HandlePacketReceived;
        }

        private void HandleSessionClosed()
        {
            S2C_InformWorldExitPacket broadcastPacket = new S2C_InformWorldExitPacket() { ClientEntityID = PlayerEntity?.ID ?? string.Empty };
            GameServer.SendAll(broadcastPacket, i => i != Session);
        }

        private void HandlePacketReceived(Packet packet)
        {
            if (World.TryGetSystem<PacketProcessSystem>(out PacketProcessSystem system) == false)
                return;

            system.Enqueue(Session, packet);
        }
    }
}