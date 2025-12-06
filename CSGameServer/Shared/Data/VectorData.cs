using MemoryPack;

namespace CSGameServer
{
    [MemoryPackable]
    public partial class VectorData
    {
        public float x;
        public float y;

        [MemoryPackConstructor]
        public VectorData() : this(0, 0) { }
        public VectorData(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}