using CSGameServer;
using UnityEngine;

public class S2C_InformWorldEnterPacketHandler : ClientPacketHandler<S2C_InformWorldEnterPacket>
{
    public S2C_InformWorldEnterPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, S2C_InformWorldEnterPacket packet)
    {
        GameObject clientEntity = Object.Instantiate(GameManager.Instance.EntityPrefab);
        GameManager.Instance.AddEntity(packet.ClientEntityID, clientEntity);
    }
}