using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class C2S_MoveInputPacket : Packet
    {
        public VectorData MoveInput { get; set; }
    }
}