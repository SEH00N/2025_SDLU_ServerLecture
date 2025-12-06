using System.Collections.Generic;

namespace CSGameServer
{
    public class PacketProcessSystem : GameSystem
    {
        public struct PacketProcessInfo
        {
            public Session session;
            public Packet packet;
            
            public PacketProcessInfo(Session session, Packet packet)
            {
                this.session = session;
                this.packet = packet;
            }
        }

        private object locker = new object();
        private Queue<PacketProcessInfo> packetQueue = null;

        public PacketProcessSystem()
        {
            packetQueue = new Queue<PacketProcessInfo>();
        }

        protected override void OnPreUpdate(float deltaTime)
        {
            base.OnPreUpdate(deltaTime);

            if (packetQueue.Count <= 0)
                return;

            lock (locker)
            {
                while (packetQueue.Count > 0)
                {
                    PacketProcessInfo packetProcessInfo = packetQueue.Dequeue();
                    IPacketHandler packetHandler = PacketManager.CreatePacketHandler(packetProcessInfo.packet.GetType());
                    if (packetHandler != null)
                        packetHandler.HandlePacket(packetProcessInfo.session, packetProcessInfo.packet);
                }
            }
        }
        
        public void Enqueue(Session session, Packet packet)
        {
            lock(locker)
            {
                packetQueue.Enqueue(new PacketProcessInfo(session, packet));
            }
        }
    }
}