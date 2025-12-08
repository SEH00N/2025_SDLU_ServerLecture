using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class S2C_InformWorldExitPacket : Packet
    {
        public string ClientEntityID { get; set; }
    }
}