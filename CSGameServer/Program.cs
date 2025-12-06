using System;

namespace CSGameServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SetUpPacketManager();

            World world = new World(30);
            world.AddSystem(new PacketProcessSystem());

            GameServer gameServer = new GameServer(world);
            gameServer.OnSessionConnectedEvent += (session) => HandleSessionConnected(gameServer, session);
            gameServer.OnSessionDisconnectedEvent += (session) => HandleSessionDisconnected(gameServer, session);

            ServerPacketHandlerData serverPacketHandlerData = new ServerPacketHandlerData(gameServer, world);
            PacketManager.Initialize(serverPacketHandlerData);

            world.StartUpdateLoop();
            gameServer.StartServer(9696);

            Console.ReadLine();
        }

        private static void SetUpPacketManager()
        {
            PacketManager.On<MessagePacket>(packetHandlerData => new MessagePacketHandler(packetHandlerData));
        }

        private static void HandleSessionConnected(GameServer gameServer, ClientSession session)
        {
            string broadcastMessage = $"Client Connected. Client ID: {session.SessionID}";
            MessagePacket broadcastPacket = new MessagePacket() {
                Message = broadcastMessage
            };

            Console.WriteLine(broadcastMessage);
            gameServer.SendAll(broadcastPacket, otherSession => otherSession != session);

            gameServer.Send(session, new MessagePacket() {
                Message = $"Welcom, {session.SessionID}!"
            });
        }

        private static void HandleSessionDisconnected(GameServer gameServer, ClientSession session)
        {
            string broadcastMessage = $"Client Disconnected. Client ID: {session.SessionID}";
            MessagePacket broadcastPacket = new MessagePacket() {
                Message = broadcastMessage
            };

            Console.WriteLine(broadcastMessage);
            gameServer.SendAll(broadcastPacket, otherSession => otherSession != session);
        }
    }
}
