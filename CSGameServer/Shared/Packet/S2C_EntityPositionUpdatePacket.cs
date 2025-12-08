using System.Collections.Generic;
using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class S2C_EntityPositionUpdatePacket : Packet
    {
        public Dictionary<string, VectorData> EntityPositions { get; set; }
    }
}