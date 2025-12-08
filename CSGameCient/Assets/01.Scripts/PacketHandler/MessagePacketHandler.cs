using CSGameServer;

public class MessagePacketHandler : ClientPacketHandler<MessagePacket>
{
    public MessagePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, MessagePacket packet)
    {
        // chatUI.AddMessage(packet.Message);
    }
}