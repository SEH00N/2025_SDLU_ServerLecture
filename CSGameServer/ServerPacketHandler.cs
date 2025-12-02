namespace CSGameServer
{
    public abstract class ServerPacketHandler<TPacket> : PacketHandler<TPacket> where TPacket : Packet
    {
        protected GameServer gameServer = null;

        public ServerPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
            ServerPacketHandlerData serverPacketHandlerData = packetHandlerData as ServerPacketHandlerData;
            gameServer = serverPacketHandlerData.GameServer;
        }
    }
}