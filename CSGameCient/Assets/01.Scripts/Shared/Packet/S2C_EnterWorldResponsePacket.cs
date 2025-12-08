using System.Collections.Generic;
using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class S2C_EnterWorldResponsePacket : Packet
    {
        public string ClientEntityID { get; set; }
        public Dictionary<string, VectorData> EntityList { get; set; }
    }
}