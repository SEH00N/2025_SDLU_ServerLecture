using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class S2C_InformWorldEnterPacket : Packet
    {
        public string ClientEntityID { get; set; }
    }
}