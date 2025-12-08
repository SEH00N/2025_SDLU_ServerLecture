using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using CSGameServer;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance = null;
    public static NetworkManager Instance => instance;

    private Session session = null;

    private object jobQueueLocker = new object();
    private Queue<Action> jobQueue = new Queue<Action>();

    public event Action OnSessionConnectedEvent = null;
    public event Action OnSessionClosedEvent = null;

    public bool IsConnected => session != null;

    private void Update()
    {
        if (jobQueue.Count <= 0)
            return;

        lock (jobQueueLocker)
        {
            while (jobQueue.Count > 0)
            {
                Action job = jobQueue.Dequeue();
                job?.Invoke();
            }
        }
    }

    public void Initialize()
    {
        if (instance != null)
        {
            DestroyImmediate(instance.gameObject);
            return;
        }

        instance = this;
    }

    public void Connect(string address, int port)
    {
        if (session != null)
        {
            Debug.LogError("Already connected!");
            return;
        }

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress ipAddress = IPAddress.Parse(address);
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

        SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        connectArgs.RemoteEndPoint = ipEndPoint;
        connectArgs.Completed += HandleConnected;

        bool isPending = socket.ConnectAsync(connectArgs);
        if (isPending == false)
            HandleConnected(null, connectArgs);
    }

    public void Disconnect()
    {
        if (session == null)
        {
            Debug.Log("Already disconnected");
            return;
        }

        session.Close();
    }

    public void Send(Packet packet)
    {
        if (session == null)
        {
            Debug.LogError($"Session is null");
            return;
        }

        if(PacketManager.TrySerializePacket(packet, out ArraySegment<byte> serializedBuffer))
            session.SendAsync(serializedBuffer);
    }

    private void HandleConnected(object sender, SocketAsyncEventArgs connectArgs)
    {
        // 연결 요청이 처리됐을 때 호출되는 함수
        if (connectArgs.SocketError != SocketError.Success)
        {
            Debug.LogError("Failed to connect");
            return;
        }

        session = new Session(connectArgs.ConnectSocket);
        session.OnSessionClosedEvent += HandleSessionClosed;
        session.OnPacketReceivedEvent += HandlePacketReceived;

        session.Open();

        lock(jobQueueLocker)
        {
            jobQueue.Enqueue(() => {
                OnSessionConnectedEvent?.Invoke();
            });
        }
    }

    private void HandleSessionClosed()
    {
        session = null;
        
        lock(jobQueueLocker)
        {
            jobQueue.Enqueue(() => OnSessionClosedEvent?.Invoke());
        }
    }

    private void HandlePacketReceived(Packet packet)
    {
        lock(jobQueueLocker)
        {
            jobQueue.Enqueue(() => {
                IPacketHandler packetHandler = PacketManager.CreatePacketHandler(packet.GetType());
                if(packetHandler != null)
                    packetHandler.HandlePacket(session, packet);
            }); 
        }
    }
}