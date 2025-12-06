using System;
using System.Collections.Generic;
using System.Text;
using MemoryPack;

namespace CSGameServer
{
    public static class PacketManager
    {
        public const int PACKET_MAX_SIZE = ushort.MaxValue;
        public const int PACKET_SIZE_HEADER = sizeof(ushort);
        private const int PACKET_TYPE_MAX_SIZE = ushort.MaxValue;
        private const int PACKET_TYPE_SIZE_HEADER = sizeof(ushort);

        private static Dictionary<string, Type> packetTypes = null;
        private static Dictionary<Type, Func<IPacketHandlerData, IPacketHandler>> packetHandlerFactories = null;

        private static IPacketHandlerData packetHandlerData = null;

        static PacketManager()
        {
            packetTypes = new Dictionary<string, Type>();
            packetHandlerFactories = new Dictionary<Type, Func<IPacketHandlerData, IPacketHandler>>();
        }

        public static void Initialize(IPacketHandlerData packetHandlerData = null)
        {
            PacketManager.packetHandlerData = packetHandlerData;
        }

        public static void On<TPacket>(Func<IPacketHandlerData, IPacketHandler> packetHandlerFactory) where TPacket : Packet
        {
            packetTypes[typeof(TPacket).Name] = typeof(TPacket);
            packetHandlerFactories[typeof(TPacket)] = packetHandlerFactory;
        }

        public static bool TrySerializePacket(Packet packet, out ArraySegment<byte> serializedBuffer)
        {
            serializedBuffer = ArraySegment<byte>.Empty;
            try 
            {
                byte[] packetTypeNameBytes = Encoding.UTF8.GetBytes(packet.GetType().Name);
                if(packetTypeNameBytes.Length > PACKET_TYPE_MAX_SIZE)
                    return false;

                byte[] packetDataBytes = MemoryPackSerializer.Serialize(packet.GetType(), packet);
                ushort packetSize = (ushort)(PACKET_SIZE_HEADER + PACKET_TYPE_SIZE_HEADER + packetTypeNameBytes.Length + packetDataBytes.Length);
                if (packetSize > PACKET_MAX_SIZE)
                    return false;

                serializedBuffer = new ArraySegment<byte>(new byte[packetSize]);
                // [ 패킷 전체 사이즈, 타입 이름 길이, 타입 이름, 데이터 ]
                Buffer.BlockCopy(BitConverter.GetBytes(packetSize), 0, serializedBuffer.Array, serializedBuffer.Offset, PACKET_SIZE_HEADER);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)packetTypeNameBytes.Length), 0, serializedBuffer.Array, serializedBuffer.Offset + PACKET_SIZE_HEADER, PACKET_TYPE_SIZE_HEADER);
                Buffer.BlockCopy(packetTypeNameBytes, 0, serializedBuffer.Array, serializedBuffer.Offset + PACKET_SIZE_HEADER + PACKET_TYPE_SIZE_HEADER, packetTypeNameBytes.Length);
                Buffer.BlockCopy(packetDataBytes, 0, serializedBuffer.Array, serializedBuffer.Offset + PACKET_SIZE_HEADER + PACKET_TYPE_SIZE_HEADER + packetTypeNameBytes.Length, packetDataBytes.Length);

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public static bool TryDeserializePacket(ArraySegment<byte> buffer, out Packet packet)
        {
            // [ 패킷 타입 이름 사이즈, 패킷 타입 이름, 패킷 데이터 ]
            packet = null;

            try
            {
                ushort packetTypeNameLength = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if(packetTypeNameLength > buffer.Count - PACKET_TYPE_SIZE_HEADER)
                    return false;

                string packetTypeName = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + PACKET_TYPE_SIZE_HEADER, packetTypeNameLength);
                Console.WriteLine($"PacketReceived: {packetTypeName}");
                if(packetTypes.TryGetValue(packetTypeName, out Type packetType) == false)
                    return false;

                ArraySegment<byte> packetData = new ArraySegment<byte>(buffer.Array, buffer.Offset + PACKET_TYPE_SIZE_HEADER + packetTypeNameLength, buffer.Count - PACKET_TYPE_SIZE_HEADER - packetTypeNameLength);
                packet = MemoryPackSerializer.Deserialize(packetType, packetData) as Packet;
                if(packet == null)
                    return false;

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public static IPacketHandler CreatePacketHandler(Type packetType)
        {
            if(packetHandlerFactories.TryGetValue(packetType, out Func<IPacketHandlerData, IPacketHandler> packetHandlerFactory) == false)
                return null;

            return packetHandlerFactory(packetHandlerData);
        }
    }
}
