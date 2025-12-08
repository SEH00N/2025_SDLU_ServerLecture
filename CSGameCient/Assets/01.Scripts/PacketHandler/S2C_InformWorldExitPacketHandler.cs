using CSGameServer;

public class S2C_InformWorldExitPacketHandler : ClientPacketHandler<S2C_InformWorldExitPacket>
{
    public S2C_InformWorldExitPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, S2C_InformWorldExitPacket packet)
    {
        GameManager.Instance.RemoveEntity(packet.ClientEntityID);
    }
}