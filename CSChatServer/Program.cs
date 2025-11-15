using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CSChatServer
{
    // 단일 소켓과의 세션을 관리하는 객체
    public class Session
    {
        // 현재 세션의 클라이언트와 연결된 소켓
        private Socket connectedSocket = null;

        // Send 관련 변수들의 원자성 보존을 위한 locker 선언.
        private object sendLocker = new object();

        // Send 관련 변수
        private bool isSending = false; // 현재 세션이 데이터를 전송중인지 나타내는 플래그 변수
        private Queue<byte[]> sendQueue = null; // 전송할 데이터를 담아두는 queue
        private SocketAsyncEventArgs sendArgs = null; // 전송을 위한 SAEA 객체

        // Receive 관련 변수
        private byte[] receiveBuffer = null; // 수신을 위한 버퍼. 수신은 한번에 하나씩의 데이터만을 수신하기 때문에 단일 버퍼를 둡니다.
        private SocketAsyncEventArgs receiveArgs = null; // 수신을 위한 SAEA 객체

        // 콜백
        private Action closedCallback = null; // 소켓이 close 될 때 호출하는 콜백
        private Action<string> receivedCallback = null; // 소켓이 데이터를 수신했을 때 호출하는 콜백

        public Session(Socket connectedSocket, Action closedCallback, Action<string> receivedCallback)
        {
            // 멤버 변수 초기화
            this.connectedSocket = connectedSocket;
            this.closedCallback = closedCallback;
            this.receivedCallback = receivedCallback;

            // Send 관련 변수 초기화
            isSending = false;
            sendQueue = new Queue<byte[]>();
            sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += HandleSent; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.SendAsync가 완료되었을 때 HandleSent가 호출되도록 합니다.

            // Receive 관련 변수 초기화
            receiveBuffer = new byte[1024];
            receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += HandleReceived; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.ReceiveAsync가 완료되었을 때 HandleReceived가 호출되도록 합니다.
        }

        // 데이터를 전송하는 함수
        public void SendAsync(byte[] buffer)
        {
            // Send 관련 멤버 변수의 원자성 보존을 위한 lock 진입
            lock (sendLocker)
            {
                // 만약 이전에 요청한 SendAsync가 완료되지 않았다면(= isSending이 true라면) sendQueue에만 추가하고 함수를 종료합니다.
                if (isSending)
                {
                    sendQueue.Enqueue(buffer);
                    return;
                }

                // 처리중인 SendAsync가 없다면(= isSending이 false라면) isSending을 true로 설정한 뒤 데이터 전송을 진행합니다.
                isSending = true;
            }

            // Send용 SAEA에 보내고자 하는 데이터를 버퍼로 설정 합니다.
            sendArgs.SetBuffer(buffer, 0, buffer.Length);

            // Send용 SAEA를 매개변수로 Socket.SendAsync를 호출합니다. Socket.SendAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
            // Socket.SendAsync가 동기적으로 처리되었다면(= Socket.SendAsync의 반환값이 false 라면) Send용 SAEA에 등록한 Completed(= HandleSent) 가 호출되지 않기 때문에 직접 HandleSent를 호출해줍니다.
            bool isPending = connectedSocket.SendAsync(sendArgs);
            if (isPending == false)
                HandleSent(null, sendArgs);
        }

        // 데이터 전송이 완료되었을 때 호출되는 함수.
        private void HandleSent(object sender, SocketAsyncEventArgs sendArgs)
        {
            // 만약 데이터 전송이 정상적으로 이루어지지 않은 경우 세션을 종료합니다.
            if (sendArgs.SocketError != SocketError.Success || sendArgs.BytesTransferred <= 0)
            {
                CloseSession();
                return;
            }

            // 추가적으로 처리해야 할 버퍼가 남아있는지를 체크합니다.
            byte[] pendingBuffer = null;

            // Send 관련 멤버 변수의 원자성 보존을 위한 lock 진입
            lock (sendLocker)
            {
                // 만약 sendQueue가 비어있지 않다면 pendingBuffe를 로드하고, 비어있다면 isSending을 false로 만들어 전송을 종료합니다.
                if (sendQueue.Count > 0)
                    pendingBuffer = sendQueue.Dequeue();
                else
                    isSending = false;
            }

            // 만약 pendingBuffer가 비어있지 않다면
            if (pendingBuffer != null && pendingBuffer.Length > 0)
            {
                // 다시 전송처리를 진행합니다. 이 때 데드락 방지를 위해 this.SendAsync를 호출하지 않고 직접 Socket.SendAsync를 호출합니다.
                sendArgs.SetBuffer(pendingBuffer, 0, pendingBuffer.Length);
                bool isPending = connectedSocket.SendAsync(sendArgs);
                if (isPending == false)
                    HandleSent(null, sendArgs);
            }
        }

        // 데이터를 수신하는 함수
        public void ReceiveAsync()
        {
            // Receive용 SAEA에 수신용 버퍼를 설정합니다. SAEA를 매개변수로 ReceiveAsync를 호출하게 되면 수신된 데이터가 SAEA에 설정된 버퍼, 즉 this.receiveBuffer에 설정됩니다.
            receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

            // Receive용 SAEA를 매개변수로 Socket.ReceiveAsync를 호출합니다. Socket.ReceiveAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
            // Socket.ReceiveAsync가 동기적으로 처리되었다면(= Socket.ReceiveAsync의 반환값이 false 라면) Receive용 SAEA에 등록한 Completed(= HandleReceived) 가 호출되지 않기 때문에 직접 HandleReceived를 호출해줍니다.
            bool isPending = connectedSocket.ReceiveAsync(receiveArgs);
            if (isPending == false)
                HandleReceived(null, receiveArgs);
        }

        // 데이터가 수신되었을 때 호출되는 함수.
        private void HandleReceived(object sender, SocketAsyncEventArgs receiveArgs)
        {
            // 만약 데이터 수신이 정상적으로 이루어지지 않은 경우 세션을 종료합니다.
            if (receiveArgs.SocketError != SocketError.Success || receiveArgs.BytesTransferred <= 0)
            {
                CloseSession();
                return;
            }

            // 수신한 데이터(= Receive용 SAEA에 설정한 버퍼의 데이터)를 UTF8로 디코딩하여 receivedCallback을 호출해줍니다.
            string message = Encoding.UTF8.GetString(receiveArgs.Buffer, receiveArgs.Offset, receiveArgs.BytesTransferred);
            receivedCallback?.Invoke(message);

            // 수신 처리가 완료되면 Receive 루프가 끊기지 않도록 ReceiveAsync를 호출해줍니다.
            ReceiveAsync();
        }

        // 세션을 종료하는 함수.
        private void CloseSession()
        {
            // 연결된 소켓을 Close 한 뒤, 사용하던 SAEA들의 메모리를 정리합니다.
            connectedSocket.Close();
            receiveArgs.Dispose();
            sendArgs.Dispose();

            // 세션이 종료되었음을 알리는 콜백을 호출합니다.
            closedCallback?.Invoke();
        }
    }

    internal class Program
    {
        // 소켓 연결을 위한 Listen용 Socket.
        private static Socket listenSocket = null;

        // 활성화된 세션을 관리하기 위한 Dictionary와 ID를 부여하기 위한 couter 선언.
        private static readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();
        private static int clientCounter = 0;

        // Session 관련 변수의 원자성 보존을 위한 locker 선언.
        private static readonly object locker = new object();

        static void Main(string[] args)
        {
            // IPv4로 바인딩하기 위한 TCP 소켓을 선언한 후 9696포트에 바인딩합니다.
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, 9696));

            // 바인딩이 완료된 소켓을 리슨상태로 전환합니다. 이 시점 이후부터 서버의 주소, 포트를 엔드포인트로 연결을 맺을 수 있게 됩니다.
            listenSocket.Listen();

            // 리슨중인 소켓으로부터의 연결 요청을 Accept하기 위한 SAEA를 선언 합니다.
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += HandleAccepted; // SocketAsync 이벤트가 완료되었을 때 호출되는 이벤트. 해당 경우 Socket.AcceptAsync가 완료되었을 때 HandleAccepted가 호출되도록 합니다.

            // 연결 요청을 수락하기 위한 Accept 루프를 시작합니다.
            AcceptAsync(acceptArgs);

            // 메인 스레드가 종료되는 것을 방지하기 위해 메인 스레드를 블럭킹합니다. 콘솔에 아무 키가 입력되면 프로세스가 종료됩니다.
            Console.ReadLine();
        }

        // 연결 요청을 수락하는 함수.
        private static void AcceptAsync(SocketAsyncEventArgs acceptArgs)
        {
            // Accept용 SAEA는 매번 새 인스턴스를 생성하지 않고 재활용됩니다. 이전 값이 남아있을 수도 있기에 AcceptSocket를 null로 초기화해줍니다.
            acceptArgs.AcceptSocket = null;

            // Accept용 SAEA를 매개변수로 Socket.AcceptAsync를 호출합니다. Socket.AcceptAsync는 요청이 동기적으로 처리되었을 경우 false, 비동기적으로 처리되었을 경우 true를 반환합니다.
            // Socket.AcceptAsync가 동기적으로 처리되었다면(= Socket.AcceptAsync의 반환값이 false 라면) Accept용 SAEA에 등록한 Completed(= HandleAccepted) 가 호출되지 않기 때문에 직접 HandleAccepted를 호출해줍니다.
            bool isPending = listenSocket.AcceptAsync(acceptArgs);
            if (isPending == false)
                HandleAccepted(null, acceptArgs);
        }

        // 연결 요청이 처리되었을 때 호출되는 함수.
        private static void HandleAccepted(object sender, SocketAsyncEventArgs acceptArgs)
        {
            // 만약 연결 요청에 대한 처리가 정상적으로 이루어지지 않은 경우 세션을 생성하지 않고 다음 연결 요청을 처리하기 위해 AcceptAsync를 호출합니다.
            if (acceptArgs.SocketError != SocketError.Success || acceptArgs.AcceptSocket == null)
            {
                AcceptAsync(acceptArgs);
                return;
            }

            // 연결된 Socket과의 세션을 생성합니다.
            Session session = null;

            // Session 관련 멤버 변수의 원자성 보존을 위한 lock 진입.
            lock (locker)
            {
                // 소켓의 아이디를 설정합니다. 사용된 카운터는 +1 해줍니다.
                int clientID = clientCounter++;

                // 소켓이 연결되었음을 알리는 메세지를 콘솔에 출력하고, 브로드캐스트합니다.
                string message = $"Socket connected. ClientID: {clientID}";
                Console.WriteLine(message);
                BroadcastMessage(message);

                // Accept된 Socket으로 Session을 생성하여 this.sessions에 추가해줍니다.
                Socket connectedSocket = acceptArgs.AcceptSocket;
                session = new Session(
                    connectedSocket,
                    () => HandleSessionClosed(clientID), // 세션이 종료되었을 때 호출되는 콜백에 HandleSessionClosed의 호출을 설정합니다.
                    (message) => HandleSessionReceived(clientID, message) // 세션이 데이터를 수신했을 때 호출되는 콜백에 HandleSessionReceived의 호출을 설정합니다.
                );

                sessions.Add(clientID, session);
            }

            // 데드락을 방지하기 위해 lock의 외부에서 세션의 Receive 루프를 시작합니다.
            session.ReceiveAsync();

            // 세션 처리가 완료되면 Accept 루프가 끊기지 않도록 AcceptAsync를 호출해줍니다.
            AcceptAsync(acceptArgs);
        }

        // 세션이 종료되었을 때 호출되는 함수
        private static void HandleSessionClosed(int clientID)
        {
            // Session 관련 멤버 변수의 원자성 보존을 위한 lock 진입.
            lock (locker)
            {
                // 종료된 세션을 세션 리스트에서 제거합니다.
                sessions.Remove(clientID);

                // 소켓이 연결이 끊겼음을 알리는 메세지를 콘솔에 출력하고, 브로드캐스트합니다.
                string message = $"Socket disconnected. ClientID: {clientID}";
                Console.WriteLine(message);
                BroadcastMessage(message);
            }
        }

        // 세션이 데이터를 수신했을 때 호출되는 함수
        private static void HandleSessionReceived(int clientID, string message)
        {
            // Session 관련 멤버 변수의 원자성 보존을 위한 lock 진입.
            lock (locker)
            {
                // 수신한 메세지를 콘솔에 출력 후 연결된 모든 소켓들에게 브로드캐스트합니다.
                string broadcastMessage = $"Client {clientID}: {message}";
                Console.WriteLine(broadcastMessage);
                BroadcastMessage(broadcastMessage);
            }
        }

        // 메세지를 연결된 모든 세션들에게 전송하는 함수
        private static void BroadcastMessage(string message)
        {
            // 메세지를 UTF8로 인코딩하여 전송하기 위한 버퍼를 생성합니다.
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            // 연결된 모든 세션들(this.sessions)을 순회하며 데이터를 전송합니다.
            foreach (int clientID in sessions.Keys)
            {
                Session session = sessions[clientID];
                session.SendAsync(buffer);
            }
        }
    }
}
