namespace CSGameServer
{
    public class ClientSessionHandle
    {
        public ClientSession Session { get; set; }
        public World World { get; set; }
        
        public ClientSessionHandle(ClientSession session, World world)
        {
            Session = session;
            World = world;
            Session.OnPacketReceivedEvent += HandlePacketReceived;
        }

        private void HandlePacketReceived(Packet packet)
        {
            if (World.TryGetSystem<PacketProcessSystem>(out PacketProcessSystem system) == false)
                return;

            system.Enqueue(Session, packet);
        }
    }
}