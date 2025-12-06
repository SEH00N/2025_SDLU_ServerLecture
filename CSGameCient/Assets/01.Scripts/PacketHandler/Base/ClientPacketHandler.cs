using CSGameServer;

public abstract class ClientPacketHandler<TPacket> : PacketHandler<TPacket> where TPacket : Packet
{
    protected ChatUI chatUI = null;

    public ClientPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
        ClientPacketHandlerData clientPacketHandlerData = packetHandlerData as ClientPacketHandlerData;
        chatUI = clientPacketHandlerData.ChatUI;
    }

    // protected abstract void OnHandlePacket(ClientSession session, TPacket packet);
    // protected sealed override void HandlePacket(Session session, TPacket packet)
    // {
    //     if (session is ClientSession clientSession == false)
    //         return;

    //     OnHandlePacket(clientSession, packet);
    // }
}