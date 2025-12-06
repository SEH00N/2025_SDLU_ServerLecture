using System.Collections.Generic;
using System.Linq;

namespace CSGameServer
{
    public class C2S_MoveInputPacketHandler : ServerPacketHandler<C2S_MoveInputPacket>
    {
        public C2S_MoveInputPacketHandler(IPacketHandlerData packetHandlerData) : base(packetHandlerData)
        {
        }

        protected override void OnHandlePacket(ClientSession session, C2S_MoveInputPacket packet)
        {
            if (gameServer.TryGetSessionHandle(session.SessionID, out ClientSessionHandle sessionHandle) == false)
                return;

            if (sessionHandle.PlayerEntity == null)
                return;

            sessionHandle.PlayerEntity.SetInput(packet.MoveInput);
        }
    }
}