using System.Collections.Generic;

namespace CSGameServer
{
    public class EntityPositionNetworkUpdateSystem : GameSystem
    {
        private const int TICK_THRESHOLD = 1;
        private int counter = 0;

        private World world = null;
        private GameServer gameServer = null;

        public EntityPositionNetworkUpdateSystem(World world, GameServer gameServer)
        {
            this.world = world;
            this.gameServer = gameServer;

            counter = 0;
        }

        protected override void OnLateUpdate(float deltaTime)
        {
            base.OnLateUpdate(deltaTime);

            counter += 1;
            if (counter < TICK_THRESHOLD)
                return;

            counter = 0;

            Dictionary<string, VectorData> entityPositions = new Dictionary<string, VectorData>();
            foreach (Entity entity in world.GetAllEntities())
                entityPositions[entity.ID] = entity.Position;

            if (entityPositions.Count <= 0)
                return;

            S2C_EntityPositionUpdatePacket broadcastPacket = new S2C_EntityPositionUpdatePacket() { EntityPositions = entityPositions };
            gameServer.SendAll(broadcastPacket);
        }
    }
}