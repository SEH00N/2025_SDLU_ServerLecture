using System;

namespace CSGameServer
{
    public class MessagePacketHandler : ServerPacketHandler<MessagePacket>
    {
        public MessagePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
        }

        protected override void HandlePacket(Session session, MessagePacket packet)
        {
            if (gameServer.TryGetSession(session.SessionID, out SessionHandle sessionHandle) == false)
                return;

            string broadcastMessage = $"Client {sessionHandle.Session.SessionID}: {packet.Message}";
            MessagePacket broadcastPacket = new MessagePacket() {
                Message = broadcastMessage
            };

            Console.WriteLine(broadcastMessage);
            gameServer.SendAll(broadcastPacket);
        }
    }
}