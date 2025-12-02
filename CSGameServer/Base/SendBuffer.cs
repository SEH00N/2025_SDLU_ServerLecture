using System;
using System.Collections.Generic;

namespace CSGameServer
{
    public class SendBuffer
    {
        private Queue<ArraySegment<byte>> bufferQueue = null;
        private List<ArraySegment<byte>> sendBufferList = null;

        public bool SendBufferValid => sendBufferList.Count > 0;
        public List<ArraySegment<byte>> SendBufferList => sendBufferList;

        public SendBuffer()
        {
            bufferQueue = new Queue<ArraySegment<byte>>();
            sendBufferList = new List<ArraySegment<byte>>();
        }

        public void Enqueue(ArraySegment<byte> buffer)
        {
            bufferQueue.Enqueue(buffer);
        }

        public void Flush()
        {
            while (bufferQueue.Count > 0)
            {
                ArraySegment<byte> buffer = bufferQueue.Dequeue();
                sendBufferList.Add(buffer);
            }
        }

        public void CleanUp()
        {
            sendBufferList.Clear();
        }
    }
}