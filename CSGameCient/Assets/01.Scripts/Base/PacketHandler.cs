namespace CSGameServer
{
    public interface IPacketHandler
    {
        void HandlePacket(Session session, Packet packet);
    }

    public abstract class PacketHandler<TPacket> : IPacketHandler where TPacket : Packet
    {
        protected IPacketHandlerData packetHandlerData = null;

        public PacketHandler(IPacketHandlerData packetHandlerData)
        {
            this.packetHandlerData = packetHandlerData;
        }

        protected abstract void HandlePacket(Session session, TPacket packet);
        void IPacketHandler.HandlePacket(Session session, Packet packet)
        {
            HandlePacket(session, packet as TPacket);
        }
    }
}