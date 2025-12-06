using System.Collections.Generic;
using System.Linq;

namespace CSGameServer
{
    public class C2S_EnterWorldRequestPacketHandler : ServerPacketHandler<C2S_EnterWorldRequestPacket>
    {
        public C2S_EnterWorldRequestPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
        }

        protected override void OnHandlePacket(ClientSession session, C2S_EnterWorldRequestPacket packet)
        {
            if (gameServer.TryGetSessionHandle(session.SessionID, out ClientSessionHandle sessionHandle) == false)
                return;

            PlayerEntity playerEntity = new PlayerEntity(sessionHandle);
            world.AddEntity(playerEntity);

            sessionHandle.PlayerEntity = playerEntity;

            S2C_InformWorldEnterPacket broadcastPacket = new S2C_InformWorldEnterPacket() { ClientEntityID = playerEntity.ID };
            gameServer.SendAll(broadcastPacket, i => i != session);

            Dictionary<string, VectorData> entityDatas = world.GetAllEntities().Where(i => i != playerEntity).ToDictionary(i => i.ID, i => i.Position);
            S2C_EnterWorldResponsePacket responsePacket = new S2C_EnterWorldResponsePacket() { ClientEntityID = playerEntity.ID, EntityList = entityDatas };
            gameServer.Send(session, responsePacket);
        }
    }
}