using System;

namespace CSGameServer
{
    public class MessagePacketHandler : ServerPacketHandler<MessagePacket>
    {
        public MessagePacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
        }

        protected override void OnHandlePacket(ClientSession session, MessagePacket packet)
        {
            if (gameServer.TryGetSession(session.SessionID, out ClientSessionHandle sessionHandle) == false)
                return;

            string broadcastMessage = $"{sessionHandle.Session.SessionID}: {packet.Message}";
            MessagePacket broadcastPacket = new MessagePacket() {
                Message = broadcastMessage
            };

            Console.WriteLine(broadcastMessage);
            gameServer.SendAll(broadcastPacket);
        }
    }
}