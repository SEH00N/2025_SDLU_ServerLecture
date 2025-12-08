using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class MessagePacket : Packet
    {
        public string Message { get; set; }
    }
}