using CSGameServer;
using UnityEngine;

public class S2C_EnterWorldResponsePacketHandler : ClientPacketHandler<S2C_EnterWorldResponsePacket>
{
    public S2C_EnterWorldResponsePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
    {
    }

    protected override void HandlePacket(Session session, S2C_EnterWorldResponsePacket packet)
    {
        foreach (string entityID in packet.EntityList.Keys)
        {
            VectorData vectorData = packet.EntityList[entityID];

            GameObject clientEntity = Object.Instantiate(GameManager.Instance.EntityPrefab);
            clientEntity.transform.position = new Vector3(vectorData.x, vectorData.y);

            GameManager.Instance.AddEntity(entityID, clientEntity);
        }

        GameObject myEntity = Object.Instantiate(GameManager.Instance.EntityPrefab);
        myEntity.AddComponent<MyEntity>();
        GameManager.Instance.AddEntity(packet.ClientEntityID, myEntity);
    }
}