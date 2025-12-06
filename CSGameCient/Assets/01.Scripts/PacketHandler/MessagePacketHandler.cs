using CSGameServer;
using UnityEngine;

public class MessagePacketHandler : ClientPacketHandler<MessagePacket>
{
    public MessagePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, MessagePacket packet)
    {
        Debug.Log(packet.Message);
        chatUI.AddMessage(packet.Message);
    }
}