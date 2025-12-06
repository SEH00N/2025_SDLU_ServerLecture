using System;

namespace CSGameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SetUpPacketManager();

            World world = new World(30);

            GameServer gameServer = new GameServer(world);
            gameServer.OnSessionConnectedEvent += (session) => HandleSessionConnected(gameServer, session);
            gameServer.OnSessionDisconnectedEvent += (session) => HandleSessionDisconnected(gameServer, session);

            ServerPacketHandlerData serverPacketHandlerData = new ServerPacketHandlerData(gameServer, world);
            PacketManager.Initialize(serverPacketHandlerData);

            world.AddSystem(new PacketProcessSystem());
            world.AddSystem(new EntityPositionNetworkUpdateSystem(world, gameServer));
            world.StartUpdateLoop();

            gameServer.StartServer(9696);

            Console.ReadLine();
        }

        private static void SetUpPacketManager()
        {
            PacketManager.On<MessagePacket>(packetHandlerData => new MessagePacketHandler(packetHandlerData));
            PacketManager.On<C2S_EnterWorldRequestPacket>(packetHandlerData => new C2S_EnterWorldRequestPacketHandler(packetHandlerData));
            PacketManager.On<C2S_MoveInputPacket>(packetHandlerData => new C2S_MoveInputPacketHandler(packetHandlerData));
        }

        private static void HandleSessionConnected(GameServer gameServer, ClientSession session)
        {
            string broadcastMessage = $"Client Connected. Client ID: {session.SessionID}";
            Console.WriteLine(broadcastMessage);
        }

        private static void HandleSessionDisconnected(GameServer gameServer, ClientSession session)
        {
            string broadcastMessage = $"Client Disconnected. Client ID: {session.SessionID}";
            Console.WriteLine(broadcastMessage);
        }
    }
}
