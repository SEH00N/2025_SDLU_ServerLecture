namespace CSGameServer
{
    public class SessionHandle
    {
        public Session Session { get; set; }
        
        public SessionHandle(Session session)
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