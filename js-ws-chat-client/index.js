// ws 모듈에서 WebSocket을 사용하기 위해 import 해줍니다.
import { WebSocket } from 'ws';

// readline 모듈을 import 해줍니다.
import readline from 'readline';

// WebSocket 인스턴스를 생성합니다.
const socket = new WebSocket('ws://localhost:9696');

// 소켓이 연결되었을 때 호출되는 이벤트와 소켓에 데이터가 수신됐을 때 호출되는 이벤트에 각각 함수의 호출을 구독합니다.
socket.on('open', () => handleSocketOpen());
socket.on('message', (data) => handleSocketMessage(data));

// 입력을 받기 위한 readline 객체를 생성합니다.
const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: '> '
});

// readline에 줄바꿈(enter)이 입력되었을 때 호출되는 이벤트와 readline에 종료(^c)가 입력되었을 때 호출되는 이벤트에 각각 함수의 호출을 구독합니다.
rl.on('line', (line) => handleInput(line.trim()));
rl.on('close', handleClose);
rl.prompt();

// 소켓이 연결되었을 때 호출되는 함수
function handleSocketOpen() {
    // 콘솔에 연결되었음을 알리는 메세지를 출력합니다.
    log('Connected to server');
}

// 소켓에 데이터가 수신됐을 때 호출되는 함수
function handleSocketMessage(data) {
    // 소켓이 받은 데이터를 문자열 형태로 변환 후 콘솔에 출력합니다.
    const message = data.toString();
    log(message);
}

// 줄바꿈(enter)이 입력되었을 때 호출되는 함수
function handleInput(message) {
    // 유저가 적었던 메세지가 콘솔에 남아있어 콘솔이 더러워지는 것을 방지하기 위해 콘솔의 커서를 위로 한 줄 이동하고, 해당 줄의 내용을 지웁니다.
    readline.moveCursor(process.stdout, 0, -1);
    readline.clearLine(process.stdout, 0);
    
    // 만약 입력한 메세지가 비어있다면 함수를 종료합니다.
    if(message.length === 0) {
        return;
    }

    // 소켓에 메세지를 송신하고 다시 입력을 받기 위한 준비를 합니다.
    socket.send(message);
    rl.prompt();
}

// 콘솔이 종료되었을 때 호출되는 함수
function handleClose() {
    // 콘솔에 종료가 입력되었을 때 프로세스를 종료시킵니다.
    process.exit(0);
}

// 콘솔에 메세지를 출력하는 함수
function log(message) {
    // 유저가 적고있는 메세지와 콘솔에 출력되는 메세지가 섞이는 것을 방지하기 위해 적고 있는 메세지를 지웁니다.
    readline.clearLine(process.stdout, 0);
    readline.cursorTo(process.stdout, 0);

    // 콘솔에 메세지를 출력합니다.
    console.log(message);

    // 다시 입력을 받기 위한 준비를 합니다.
    // 이 때 prompt의 매개변수를 true로 설정하여 기존에 적고있던 메세지(위에서 지웠던 줄의 내용)을 복구시킵니다.
    rl.prompt(true);
}