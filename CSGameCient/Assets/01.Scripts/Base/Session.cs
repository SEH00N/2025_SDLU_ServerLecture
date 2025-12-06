using System;
using System.Net.Sockets;

namespace CSGameServer
{
    public class Session
    {
        private Socket connectedSocket = null;

        private object sendLocker = new object();
        private SendBuffer sendBuffer = null;
        private SocketAsyncEventArgs sendArgs = null;

        private ReceiveBuffer receiveBuffer = null;
        private SocketAsyncEventArgs receiveArgs = null;

        public event Action OnSessionClosedEvent = null;
        public event Action<Packet> OnPacketReceivedEvent = null;

        public Session(Socket connectedSocket)
        {
            this.connectedSocket = connectedSocket;
        }

        public void Open()
        {
            sendBuffer = new SendBuffer();
            sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += HandleSent;

            receiveBuffer = new ReceiveBuffer(PacketManager.PACKET_MAX_SIZE);
            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += HandleReceived;

            ReceiveAsync();
        }

        public void Close()
        {
            try
            {
                connectedSocket.Close();
            }
            catch { }
            finally
            {
                receiveArgs.Dispose();
                sendArgs.Dispose();

                OnSessionClosedEvent?.Invoke();
            }
        }

        public void SendAsync(ArraySegment<byte> buffer)
        {
            if(sendBuffer == null)
            {
                throw new Exception("Session is not open");
            }

            if(connectedSocket.Connected == false)
            {
                Close();
                return;
            }
            
            lock (sendLocker)
            {
                sendBuffer.Enqueue(buffer);
                if (sendBuffer.SendBufferValid)
                    return;

                sendBuffer.Flush();
            }

            if (sendBuffer.SendBufferValid)
            {
                sendArgs.BufferList = sendBuffer.SendBufferList;
                bool isPending = connectedSocket.SendAsync(sendArgs);
                if (isPending == false)
                    HandleSent(null, sendArgs);
            }
        }

        private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
        {
            if (sendArgs.SocketError != SocketError.Success || sendArgs.BytesTransferred <= 0)
            {
                Close();
                return;
            }

            lock (sendLocker)
            {
                sendBuffer.CleanUp();
                sendBuffer.Flush();
            }

            if (sendBuffer.SendBufferValid)
            {
                sendArgs.BufferList = sendBuffer.SendBufferList;
                bool isPending = connectedSocket.SendAsync(sendArgs);
                if (isPending == false)
                    HandleSent(null, sendArgs);
            }
        }

        private void ReceiveAsync()
        {
            if(connectedSocket.Connected == false)
            {
                Close();
                return;
            }

            receiveBuffer.CleanUp();
            receiveArgs.SetBuffer(receiveBuffer.FreeBuffer);

            bool isPending = connectedSocket.ReceiveAsync(receiveArgs);
            if (isPending == false)
                HandleReceived(null, receiveArgs);
        }

        private void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
        {
            if (receiveArgs.SocketError != SocketError.Success || receiveArgs.BytesTransferred <= 0)
            {
                Close();
                return;
            }

            receiveBuffer.MoveWriteIndex(receiveArgs.BytesTransferred);
            int processedSize = HandlePacket(receiveBuffer.UsedBuffer);
            receiveBuffer.MoveReadIndex(processedSize);

            ReceiveAsync();
        }

        private int HandlePacket(ArraySegment<byte> buffer)
        {
            if(buffer.Count < PacketManager.PACKET_SIZE_HEADER)
                return 0;

            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            if(packetSize > PacketManager.PACKET_MAX_SIZE || packetSize > buffer.Count)
                return 0;
            
            ArraySegment<byte> packetData = new ArraySegment<byte>(buffer.Array, buffer.Offset + PacketManager.PACKET_SIZE_HEADER, packetSize - PacketManager.PACKET_SIZE_HEADER);
            if(PacketManager.TryDeserializePacket(packetData, out Packet packet))
            {
                if(packet != null)
                    OnPacketReceivedEvent?.Invoke(packet);
            }

            return packetSize;
        }
    }
}