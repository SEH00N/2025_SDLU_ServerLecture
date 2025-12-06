namespace CSGameServer
{
    public class ServerPacketHandlerData : IPacketHandlerData
    {
        public GameServer GameServer { get; set; }

        public ServerPacketHandlerData(GameServer gameServer)
        {
            this.GameServer = gameServer;
        }
    }
}