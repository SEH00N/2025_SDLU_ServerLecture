namespace CSGameServer
{
    public class ServerPacketHandlerData : IPacketHandlerData
    {
        public GameServer GameServer { get; set; }
        public World World { get; set; }

        public ServerPacketHandlerData(GameServer gameServer, World world)
        {
            GameServer = gameServer;
            World = world;
        }
    }
}