using System;

namespace CSGameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SetUpPacketManager();

            GameServer gameServer = new GameServer();

            ServerPacketHandlerData serverPacketHandlerData = new ServerPacketHandlerData(gameServer);
            PacketManager.Initialize(serverPacketHandlerData);

            gameServer.StartServer(9696);

            Console.ReadLine();
        }

        private static void SetUpPacketManager()
        {
            PacketManager.On<MessagePacket>(packetHandlerData => new MessagePacketHandler(packetHandlerData));
        }
    }
}
