namespace CSGameServer
{
    public abstract class ServerPacketHandler<TPacket> : PacketHandler<TPacket> where TPacket : Packet
    {
        protected GameServer gameServer = null;
        protected World world = null;

        public ServerPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
            ServerPacketHandlerData serverPacketHandlerData = packetHandlerData as ServerPacketHandlerData;
            gameServer = serverPacketHandlerData.GameServer;
            world = serverPacketHandlerData.World;
        }

        protected abstract void OnHandlePacket(ClientSession session, TPacket packet);
        protected sealed override void HandlePacket(Session session, TPacket packet)
        {
            if (session is ClientSession clientSession == false)
                return;

            OnHandlePacket(clientSession, packet);
        }
    }
}