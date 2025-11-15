using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class CSChatClient : MonoBehaviour
{
    [SerializeField] string address = "127.0.0.1";
    [SerializeField] int port = 9696;

    [Space(10f)]
    [SerializeField] TMP_InputField inputField = null;

    private Socket socket = null;

    private byte[] receiveBuffer = new byte[1024];

    private SocketAsyncEventArgs sendArgs = null;
    private bool isSending = false;

    private object locker = new object();
    private Queue<Action> jobQueue = new Queue<Action>();

    private void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPAddress ipAddress = IPAddress.Parse(address);
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

        SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        connectArgs.Completed += HandleConnected;
        connectArgs.RemoteEndPoint = ipEndPoint;

        sendArgs = new SocketAsyncEventArgs();
        sendArgs.Completed += HandleSent;

        bool isPending = socket.ConnectAsync(connectArgs);
        if (isPending == false)
            HandleConnected(null, connectArgs);
    }

    private void Update()
    {
        lock (locker)
        {
            while (jobQueue.Count > 0)
            {
                jobQueue.Dequeue()?.Invoke();
            }
        }
    }

    private void HandleConnected(object sender, SocketAsyncEventArgs connectArgs)
    {
        if (connectArgs.SocketError != SocketError.Success)
        {
            Debug.LogError($"Error occured during connecting socket {connectArgs.SocketError}");
            return;
        }

        SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
        receiveArgs.Completed += HandleReceived;

        ReceiveAsync(receiveArgs);
    }

    private void ReceiveAsync(SocketAsyncEventArgs receiveArgs)
    {
        receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
        bool isPending = socket.ReceiveAsync(receiveArgs);
        if (isPending == false)
            HandleReceived(null, receiveArgs);
    }

    private void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
    {
        if (receiveArgs.SocketError != SocketError.Success || receiveArgs.BytesTransferred <= 0)
        {
            socket.Close();
            return;
        }

        string message = Encoding.UTF8.GetString(receiveArgs.Buffer, 0, receiveArgs.BytesTransferred);
        lock (locker)
        {
            jobQueue.Enqueue(() => Debug.Log(message));
        }

        ReceiveAsync(receiveArgs);
    }

    public void Send()
    {
        if (socket.Connected == false)
            return;

        if (isSending == true)
            return;

        if (inputField.text.Length <= 0)
            return;

        isSending = true;

        byte[] buffer = Encoding.UTF8.GetBytes(inputField.text);
        inputField.text = "";

        sendArgs.SetBuffer(buffer, 0, buffer.Length);

        bool isPending = socket.SendAsync(sendArgs);
        if (isPending == false)
            HandleSent(null, sendArgs);
    }

    private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
    {
        isSending = false;
    }
}
