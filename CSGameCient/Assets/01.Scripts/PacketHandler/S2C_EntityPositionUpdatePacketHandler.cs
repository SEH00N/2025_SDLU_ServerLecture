using CSGameServer;
using UnityEngine;

public class S2C_EntityPositionUpdatePacketHandler : ClientPacketHandler<S2C_EntityPositionUpdatePacket>
{
    public S2C_EntityPositionUpdatePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, S2C_EntityPositionUpdatePacket packet)
    {
        foreach(string clientID in packet.EntityPositions.Keys)
        {
            if (GameManager.Instance.TryGetEntity(clientID, out GameObject entity) == false)
                continue;

            VectorData vectorData = packet.EntityPositions[clientID];
            Vector3 entityPosition = entity.transform.position;
            entityPosition.x = vectorData.x;
            entityPosition.y = vectorData.y;
            entity.transform.position = entityPosition;
        }
    }
}