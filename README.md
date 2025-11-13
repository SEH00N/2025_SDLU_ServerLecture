# <center>2025 서버개발노동조합 멘토링</center>

## 목차
> 11월 19일 (수)
 - node js, ws 을 활용하여 데이터 송수신 개념 익히기
   - 데이터 송수신 개념 익히기
   - 채팅 서버 제작
 - C# TCP Socket 을 활용하여 데이터 송수신 구현하기
   - 데이터 송수신 구현
   - 채팅 서버 제작

> 11월 20일 (목)
 - C# TCP Socket 을 활용하여 채팅 서버 구현하기
   - 채널 개설
   - 채널 참가
   - 채널 퇴장
   - 채팅

> 11월 26일 (수)
 - C# 콘솔, C# TCP Socket, Unity를 활용하여 술래잡기 게임 구현하기 
   - 월드 구현
   - 충돌 구현
   - 이동 구현

> 11월 27일 (목)
 - C# 콘솔, C# TCP Socket, Unity를 활용하여 술래잡기 게임 구현하기 
   - 서버 연결 구현
   - 위치 동기회 구현

> 12월 3일 (수)
 - C# 콘솔, C# TCP Socket, Unity를 활용하여 술래잡기 게임 구현하기
   - 술래잡기 규칙 구현 - 술래 선정
   - 술래잡기 규칙 구현 - 탈락 & 탈락 후 술래 지정

> 12월 4일(목)
 - C# 콘솔, C# TCP Socket, Unity를 활용하여 술래잡기 게임 고도화 하기
   - 각자 추가하고 싶은 기능 기획
   - 각자 추가하고 싶은 기능 구현

> 12월 11일 (목)
 - C# 콘솔, C# TCP Socket, Unity를 활용하여 술래잡기 게임 고도화 하기
   - 각자 추가하고 싶은 기능 기획
   - 각자 추가하고 싶은 기능 구현

## 11월 19일 (수)

### 환경 설정
1. node 설치
2. vscode 설치
3. vscode 확장 ESLint 설치

### 서버 프로젝트 설정
1. 서버 프로젝트로 사용할 디렉토리 생성 & 터미널 열기
2. 터미널에 `npm init --yes` 입력
   - npm(node package manager)를 사용하기 위해서 npm을 초기화해줍니다.
3. `package.json`에 `"type": "module"` 추가
   - node.js에는 크게 CommonJS와 ESModule로 나뉘는데, 본 문서에서는 ESMoudle 환경에서의 node.js를 사용하기 때문에 이를 package.json에 명시해줍니다.
4. 터미널에 `npm install ws` 입력
   - 본 문서에서는 node.js ws를 사용하여 실시간 통신을 구현하기 때문에 npm에서 ws 모듈을 다운로드 해줍니다.
5. `index.js` 파일 생성
   - 스크립트를 작성할 index.js 파일을 생성합니다.

### 서버 코드 작성
js-ws-chat-server 참고

### 클라이언트 프로젝트 설정
1. 클라이언트 프로젝트로 사용할 디렉토리 생성 & 터미널 열기
2. 터미널에 `npm init --yes` 입력
   - npm(node package manager)를 사용하기 위해서 npm을 초기화해줍니다.
3. `package.json`에 `"type": "module"` 추가
   - node.js에는 크게 CommonJS와 ESModule로 나뉘는데, 본 문서에서는 ESMoudle 환경에서의 node.js를 사용하기 때문에 이를 package.json에 명시해줍니다.
4. 터미널에 `npm install ws` 입력
   - 본 문서에서는 node.js ws를 사용하여 실시간 통신을 구현하기 때문에 npm에서 ws 모듈을 다운로드 해줍니다.
5. `index.js` 파일 생성
   - 스크립트를 작성할 index.js 파일을 생성합니다.

### 클라이언트 코드 작성
js-ws-chat-client 참고

### 테스트
1. js-ws-chat-server 디렉토리에서 새 터미널 열기
2. 터미널에 `node index.js` 입력
   - 위 명령어를 통해 `js-ws-chat-server/index.js`에 적힌 코드를 node 런타임으로 실행합니다.
3. js-ws-chat-client 디렉토리에서 두개의 새 터미널 열기
4. 각 터미널에 `node index.js` 입력
   - 위 명령어를 통해 `js-ws-chat-server/index.js`에 적힌 코드를 node 런타임으로 실행합니다.
5. 각 클라이언트 코드를 node로 실행한 터미널에 메세지 입력
   - 각 클라이언트 콘솔에 메세지를 입력하여 A콘솔에서 보낸 메세지가 B콘솔로 전달 되었는지, B콘솔에서 보낸 메세지가 A콘솔로 전달 되었는지 확인합니다.