using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CSChatServer
{
    public class Session
    {
        private Socket connectedSocket = null;

        private object sendLocker = new object();

        private bool isSending = false;
        private Queue<byte[]> sendQueue = null;
        private SocketAsyncEventArgs sendArgs = null;

        private SocketAsyncEventArgs receiveArgs = null;
        private byte[] receiveBuffer = null;

        private Action closedCallback = null;
        private Action<string> receivedCallback = null;

        public Session(Socket connectedSocket, Action closedCallback, Action<string> receivedCallback)
        {
            this.connectedSocket = connectedSocket;
            this.closedCallback = closedCallback;
            this.receivedCallback = receivedCallback;

            isSending = false;
            sendQueue = new Queue<byte[]>();
            sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += HandleSent;

            receiveBuffer = new byte[1024];
            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += HandleReceived;

            ReceiveAsync();
        }

        public void SendAsync(byte[] buffer)
        {
            lock (sendLocker)
            {
                if (isSending)
                {
                    sendQueue.Enqueue(buffer);
                    return;
                }

                isSending = true;
            }

            sendArgs.SetBuffer(buffer, 0, buffer.Length);
            bool isPending = connectedSocket.SendAsync(sendArgs);
            if (isPending == false)
                HandleSent(null, sendArgs);
        }

        private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
        {
            if (sendArgs.SocketError != SocketError.Success || sendArgs.BytesTransferred <= 0)
            {
                CloseSession();
                return;
            }

            byte[] pendingBuffer = null;
            lock (sendLocker)
            {
                if (sendQueue.Count > 0)
                    pendingBuffer = sendQueue.Dequeue();
                else
                    isSending = false;
            }

            if (pendingBuffer != null && pendingBuffer.Length > 0)
            {
                sendArgs.SetBuffer(pendingBuffer, 0, pendingBuffer.Length);
                bool isPending = connectedSocket.SendAsync(sendArgs);
                if (isPending == false)
                    HandleSent(null, sendArgs);
            }
        }

        private void ReceiveAsync()
        {
            receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

            bool isPending = connectedSocket.ReceiveAsync(receiveArgs);
            if (isPending == false)
                HandleReceived(null, receiveArgs);
        }

        private void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
        {
            if (receiveArgs.SocketError != SocketError.Success || receiveArgs.BytesTransferred <= 0)
            {
                CloseSession();
                return;
            }

            string message = Encoding.UTF8.GetString(receiveArgs.Buffer, receiveArgs.Offset, receiveArgs.BytesTransferred);
            receivedCallback?.Invoke(message);

            ReceiveAsync();
        }

        private void CloseSession()
        {
            connectedSocket.Close();
            receiveArgs.Dispose();
            sendArgs.Dispose();

            closedCallback?.Invoke();
        }
    }

    internal class Program
    {
        private static Socket listenSocket = null;

        private static readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();
        private static int clientCounter = 0;

        private static readonly object locker = new object();

        static void Main(string[] args)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, 9696));
            listenSocket.Listen();

            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += HandleAccepted;

            AcceptAsync(acceptArgs);

            Console.ReadLine();
        }

        private static void AcceptAsync(SocketAsyncEventArgs acceptArgs)
        {
            acceptArgs.AcceptSocket = null;

            bool isPending = listenSocket.AcceptAsync(acceptArgs);
            if (isPending == false)
                HandleAccepted(null, acceptArgs);
        }

        private static void HandleAccepted(object sender, SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs.SocketError != SocketError.Success || acceptArgs.AcceptSocket == null)
            {
                AcceptAsync(acceptArgs);
                return;
            }

            lock (locker)
            {
                int clientID = clientCounter++;

                string message = $"Socket connected. ClientID: {clientID}";
                Console.WriteLine(message);
                BroadcastMessage(message);

                Socket connectedSocket = acceptArgs.AcceptSocket;
                sessions.Add(clientID, new Session(
                    connectedSocket,
                    () => HandleSocketClosed(clientID),
                    (message) =>
                    {
                        string broadcastMessage = $"Client {clientID}: {message}";
                        Console.WriteLine(broadcastMessage);

                        lock (locker)
                        {
                            BroadcastMessage(broadcastMessage);
                        }
                    }
                ));
            }

            acceptArgs.AcceptSocket = null;
            AcceptAsync(acceptArgs);
        }

        private static void HandleSocketClosed(int clientID)
        {
            lock (locker)
            {
                sessions.Remove(clientID);

                string message = $"Socket disconnected. ClientID: {clientID}";
                Console.WriteLine(message);
                BroadcastMessage(message);
            }
        }

        private static void BroadcastMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (int clientID in sessions.Keys)
            {
                Session session = sessions[clientID];
                session.SendAsync(buffer);
            }
        }
    }
}
