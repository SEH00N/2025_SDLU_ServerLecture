using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class CSChatClient : MonoBehaviour
{
    // 소켓 엔드포인트 관련 변수. 유니티 인스펙터에서 수정이 가능하도록 SerializeField 어트리뷰트를 설정해줍니다.
    [SerializeField] string address = "127.0.0.1";
    [SerializeField] int port = 9696;

    // 입출력 관련 변수. 유니티 인스펙에서 설정이 가능하도록 SerializeField 어트리뷰트를 설정합니다.
    [SerializeField] TMP_Text messageText = null;
    [SerializeField] TMP_InputField inputField = null;

    // 서버와 연결되는 소켓
    private Socket socket = null;

    // Send 관련 변수
    private bool isSending = false; // 현재 소켓이 데이터를 전송중인지 나타내는 플래그 변수
    private SocketAsyncEventArgs sendArgs = null; // 전송을 위한 SAEA 객체

    // Receive 관련 변수
    private byte[] receiveBuffer = new byte[1024]; // 수신을 위한 버퍼. 수신은 한번에 하나씩의 데이터만을 수신하기 때문에 단일 버퍼를 둡니다.
    private SocketAsyncEventArgs receiveArgs = null; // 수신을 위한 SAEA 객체

    // Unity MainThread 동기화를 위한 변수
    private object locker = new object(); // Unity MainThread 동기화 관련 변수들의 원자성 보존을 위한 locker 선언.
    private Queue<Action> jobQueue = new Queue<Action>(); // 반드시 Unity MainThread 에서 돌아야 하는 작업들을 담아두는 큐 선언. 백그라운드 스레드에서 유니티의 라이프사이클에 영향을 받는 객체들을 건들여서는 안 됩니다.

    private void Start()
    {
        // IPv4로 연결하기 위한 TCP 소켓을 선언합니다.
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // 연결할 엔드포인트를 구성합니다. (this.address:this.port)
        IPAddress ipAddress = IPAddress.Parse(address);
        IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

        // 서버 소켓과 연결하기 위해 Connect용 SAEA를 선언합니다.
        SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        connectArgs.Completed += HandleConnected; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.ConnectAsync가 완료되었을 때 HandleConnected가 호출되도록 합니다.
        connectArgs.RemoteEndPoint = ipEndPoint; // Connect할 EndPoint를 설정합니다. (this.address:this.port)

        // Connect용 SAEA를 매개변수로 Socket.ConnectAsync를 호출합니다. Socket.ConnectAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
        // Socket.ConnectAsync가 동기적으로 처리되었다면(= Socket.ConnectAsync의 반환값이 false 라면) Connect용 SAEA에 등록한 Completed(= HandleConnected) 가 호출되지 않기 때문에 직접 HandleConnected를 호출해줍니다.
        bool isPending = socket.ConnectAsync(connectArgs);
        if (isPending == false)
            HandleConnected(null, connectArgs);
    }

    private void Update()
    {
        // 매 프레임 lock을 거는 행위는 CPU 부담으로 다가올 수 있기 때문에 jobQueue.Count가 0이라면 Update 함수를 조기 종료합니다.
        if (jobQueue.Count <= 0)
            return;

        // jobQueue의 원자성 보존을 위한 lock 진입.
        lock (locker)
        {
            // jobQueue에 쌓인 모든 작업들을 Unity MainThread에서 플러시해줍니다.
            while (jobQueue.Count > 0)
            {
                jobQueue.Dequeue()?.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        // 연결된 소켓을 Close 한 뒤, 사용하던 SAEA들의 메모리를 정리합니다.
        try
        {
            socket?.Close();
        }
        catch { }
        finally
        {
            receiveArgs?.Dispose();
            sendArgs?.Dispose();
        }
    }

    // 소켓 연결 요청이 처리되었을 때 호출되는 함수
    private void HandleConnected(object sender, SocketAsyncEventArgs connectArgs)
    {
        // 만약 연결 요청에 대한 처리가 정상적으로 이루어지지 않은 경우 에러를 출력하고 함수를 종료합니다.
        if (connectArgs.SocketError != SocketError.Success)
        {
            Debug.LogError($"Error occured during connecting socket {connectArgs.SocketError}");
            return;
        }

        // Send용 SAEA를 선언합니다.
        sendArgs = new SocketAsyncEventArgs();
        sendArgs.Completed += HandleSent; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.SendAsync가 완료되었을 때 HandleSent가 호출되도록 합니다.

        // Receive용 SAEA를 선언합니다.
        receiveArgs = new SocketAsyncEventArgs();
        receiveArgs.Completed += HandleReceived; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.ReceiveAsync가 완료되었을 때 HandleReceived가 호출되도록 합니다.

        // 연결 처리가 완료되었다면 Receive 루프를 시작합니다.
        ReceiveAsync();
    }

    // 데이터를 수신하는 함수
    private void ReceiveAsync()
    {
        // Receive용 SAEA에 수신용 버퍼를 설정합니다. SAEA를 매개변수로 ReceiveAsync를 호출하게 되면 수신된 데이터가 SAEA에 설정된 버퍼, 즉 this.receiveBuffer에 설정됩니다.
        receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

        // Receive용 SAEA를 매개변수로 Socket.ReceiveAsync를 호출합니다. Socket.ReceiveAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
        // Socket.ReceiveAsync가 동기적으로 처리되었다면(= Socket.ReceiveAsync의 반환값이 false 라면) Receive용 SAEA에 등록한 Completed(= HandleReceived) 가 호출되지 않기 때문에 직접 HandleReceived를 호출해줍니다.
        bool isPending = socket.ReceiveAsync(receiveArgs);
        if (isPending == false)
            HandleReceived(null, receiveArgs);
    }

    // 데이터가 수신되었을 때 호출되는 함수.
    private void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
    {
        // 만약 데이터 수신이 정상적으로 이루어지지 않은 경우 소켓을 닫습니다.
        if (receiveArgs.SocketError != SocketError.Success || receiveArgs.BytesTransferred <= 0)
        {
            socket.Close();
            return;
        }

        // 수신받은 데이터를 UTF8로 디코딩합니다.
        string message = Encoding.UTF8.GetString(receiveArgs.Buffer, receiveArgs.Offset, receiveArgs.BytesTransferred);

        // jobQueue의 원자성 보존을 위한 lock 진입.
        lock (locker)
        {
            // messageText에 수신받은 메세지를 업데이트하는 작업을 jobQueue에 등록합니다.
            jobQueue.Enqueue(() => messageText.text += $"\n{message}");
        }

        // 수신 처리가 완료되면 Receive 루프가 끊기지 않도록 ReceiveAsync를 호출해줍니다.
        ReceiveAsync();
    }

    // 데이터를 전송하는 함수
    public void Send()
    {
        // 예외처리
        if (socket.Connected == false)
            return;

        if (sendArgs == null)
            return;

        if (isSending == true)
            return;

        if (inputField.text.Length <= 0)
            return;

        // 모든 예외처리를 통과했다면 isSending을 true로 설정합니다.
        isSending = true;

        // 메세지를 UTF8로 인코딩하여 전송하기 위한 버퍼를 생성한 후 인풋 필드를 초기화합니다.
        byte[] buffer = Encoding.UTF8.GetBytes(inputField.text);
        inputField.text = "";

        // Send용 SAEA에 보내고자 하는 데이터를 버퍼로 설정 합니다.
        sendArgs.SetBuffer(buffer, 0, buffer.Length);

        // Send용 SAEA를 매개변수로 Socket.SendAsync를 호출합니다. Socket.SendAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
        // Socket.SendAsync가 동기적으로 처리되었다면(= Socket.SendAsync의 반환값이 false 라면) Send용 SAEA에 등록한 Completed(= HandleSent) 가 호출되지 않기 때문에 직접 HandleSent를 호출해줍니다.
        bool isPending = socket.SendAsync(sendArgs);
        if (isPending == false)
            HandleSent(null, sendArgs);
    }

    // 데이터 전송이 완료되었을 때 호출되는 함수.
    private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
    {
        // 만약 데이터 전송이 정상적으로 이루어지지 않은 경우 소켓을 닫습니다.
        if (sendArgs.SocketError != SocketError.Success || sendArgs.BytesTransferred <= 0)
        {
            socket.Close();
            return;
        }

        // 데이터 전송이 정상적으로 처리되었다면 isSending을 false로 만들어 전송을 종료합니다.
        isSending = false;
    }
}
