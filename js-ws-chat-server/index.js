// ws 모듈에서 WebSocketServer을 사용하기 위해 import 해줍니다.
import { WebSocketServer } from 'ws';

// 연결된 소켓을 담을 Set과 소켓의 아이디를 할당해주기 위한 카운터 변수를 선언합니다.
const connectedSockets = new Set(); // Set은 중복된 값을 허용하지 않는 자료구조입니다. C#에서의 HashSet과 같이 동작합니다.
let clientCounter = 0; // 소켓이 접속할 때마다 +1 시켜줄 카운터입니다. 값이 변하는 변수이기 때문에 let으로 선언합니다.

// WebSocketServer 인스턴스를 생성합니다. port : 9696
const socketServer = new WebSocketServer({ port: 9696 });

// 소켓의 연결이 발생할 때마다 호출되는 이벤트에 handleSocketConnection의 호출을 구독합니다.
socketServer.on('connection', (socket) => handleSocketConnection(socket));

// 소켓의 연결이 발생할 때마다 호출되는 함수
function handleSocketConnection(socket) {
    // 소켓의 아이디를 할당해줍니다. 사용된 카운터는 +1 해줍니다.
    socket.id = clientCounter++;

    // 소켓이 연결되었음을 알리는 메세지를 콘솔에 출력하고, 브로드캐스트합니다.
    const message = `Socket connected. ClientID: ${socket.id}`;
    console.log(message);
    broadcastMessage(message); // 소켓이 연결되었음을 알리는 메세지가 본인한테는 전송되지 않도록 Set에 추가하기 전에 브로드캐스트 해줍니다.

    // 연결된 소켓을 Set에 추가합니다.
    connectedSockets.add(socket);

    // 소켓에 데이터가 수신됐을 때 호출되는 이벤트와 소켓의 연결이 끊겼을 때 호출되는 이벤트에 각각 함수의 호출을 구독합니다.
    socket.on('message', (data) => handleSocketMessage(socket, data));
    socket.on('close', () => handleSocketClose(socket));
}

// 소켓에 데이터가 수신됐을 때 호출되는 함수
function handleSocketMessage(socket, data) {
    // 소켓이 받은 데이터를 문자열 형태로 변환하여(data.toString()) 브로드캐스트할 메세지를 생성합니다.
    const message = `Client ${socket.id}: ${data.toString()}`;

    // 콘솔에 출력 후 연결된 모든 소켓들에게 브로드캐스트합니다.
    console.log(message);
    broadcastMessage(message);
}

// 소켓의 연결이 끊겼을 때 호출되는 함수
function handleSocketClose(socket) {
    // 연결된 소켓을 Set에서 제거합니다.
    connectedSockets.delete(socket);

    // 소켓이 연결이 끊겼음을 알리는 메세지를 콘솔에 출력하고, 브로드캐스트합니다.
    const message = `Socket disconnected. ClientID: ${socket.id}`;
    console.log(message);
    broadcastMessage(message);
}

// 메세지를 연결된 모든 소켓들에게 전송하는 함수
function broadcastMessage(message) {
    // 연결된 모든 소켓들(connectedSockets)을 순회하며 메세지를 전송합니다.
    for(const connectedSocket of connectedSockets) {
        connectedSocket.send(message);
    }
}