namespace CSGameServer
{
    public class ClientSessionHandle
    {
        public ClientSession Session { get; set; }
        
        public ClientSessionHandle(ClientSession session)
        {
            Session = session;
            Session.OnPacketReceivedEvent += HandlePacketReceived;
        }

        private void HandlePacketReceived(Packet packet)
        {
            IPacketHandler packetHandler = PacketManager.CreatePacketHandler(packet.GetType());
            if(packetHandler != null)
                packetHandler.HandlePacket(Session, packet);
        }
    }
}