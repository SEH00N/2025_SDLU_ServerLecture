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
            string broadcastMessage = $"{session.SessionID}: {packet.Message}";
            MessagePacket broadcastPacket = new MessagePacket() {
                Message = broadcastMessage
            };

            Console.WriteLine(broadcastMessage);
            gameServer.SendAll(broadcastPacket);
        }
    }
}