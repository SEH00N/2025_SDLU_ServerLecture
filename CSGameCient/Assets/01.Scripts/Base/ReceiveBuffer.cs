using System;

namespace CSGameServer
{
    public class ReceiveBuffer
    {
        private byte[] buffer = null;
        private int readIndex = 0;
        private int writeIndex = 0;

        public ArraySegment<byte> FreeBuffer => new ArraySegment<byte>(buffer, writeIndex, buffer.Length - writeIndex);
        public ArraySegment<byte> UsedBuffer => new ArraySegment<byte>(buffer, readIndex, writeIndex - readIndex);

        public ReceiveBuffer(int size)
        {
            buffer = new byte[size];
            readIndex = 0;
            writeIndex = 0;
        }

        public void MoveWriteIndex(int count)
        {
            writeIndex = Math.Min(writeIndex + count, buffer.Length);
        }

        public void MoveReadIndex(int count)
        {
            readIndex = Math.Min(readIndex + count, writeIndex);
        }

        public void CleanUp()
        {
            int dataSize = writeIndex - readIndex;
            if (dataSize != 0)
            {
                Array.Copy(buffer, readIndex, buffer, 0, dataSize);
            }

            readIndex = 0;
            writeIndex = dataSize;
        }
    }
}