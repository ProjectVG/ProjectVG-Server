# MainAPI Server

실시간 AI 챗봇 서버로, WebSocket과 HTTP API를 통해 클라이언트와 통신하며 LLM(Large Language Model)과 벡터 메모리 저장소를 연동하는 시스템입니다.

## 프로젝트 개요

MainAPI Server는 개인화된 AI 어시스턴트를 구현한 실시간 챗봇 서버입니다. 사용자와의 대화를 기억하고, 컨텍스트를 고려한 맞춤형 응답을 제공합니다.

### 핵심 특징
- 실시간 WebSocket 통신
- 벡터 기반 장기 기억 시스템
- 세션별 대화 기록 관리
- 개인화된 AI 페르소나 지원

## 시스템 아키텍처

```
Client ↔ WebSocket/HTTP API ↔ MainAPI Server ↔ External Services
                                    ↓
                            ┌─────────────┬─────────────┐
                            │   LLM API   │ Memory API  │
                            │ (Port 5601) │ (Port 5602) │
                            └─────────────┴─────────────┘
```

## 프로젝트 구조

```
MainAPI Server/
├── Controllers/           # API 엔드포인트
│   ├── ChatController.cs  # 채팅 요청 처리
│   └── HelloController.cs # 헬스체크
├── Services/              # 비즈니스 로직
│   ├── Chat/              # 채팅 서비스
│   ├── LLM/               # LLM 연동 서비스
│   ├── Conversation/      # 대화 기록 관리
│   └── Session/           # 세션 관리
├── Clients/               # 외부 서비스 연동
│   ├── LLM/               # LLM API 클라이언트
│   └── Memory/            # 메모리 저장소 클라이언트
├── Middlewares/           # 미들웨어
│   └── WebSocketMiddleware.cs # WebSocket 처리
├── Models/                # 데이터 모델
│   ├── Request/           # API 요청 모델
│   ├── Response/          # API 응답 모델
│   ├── Chat/              # 채팅 모델
│   └── External/          # 외부 서비스 모델
└── Config/                # 설정
    └── LLMSettings.cs     # LLM 설정
```

## 핵심 기능

### 1. 실시간 통신 시스템
- WebSocket을 통한 양방향 실시간 통신
- 세션 기반 연결 관리 및 자동 재연결
- 비동기 메시지 처리

### 2. 대화 기록 관리
- 세션별 대화 기록 저장 (최대 50개 메시지)
- 시간순 정렬 및 효율적인 검색
- 메모리 기반 임시 저장

### 3. 장기 기억 시스템
- 벡터 데이터베이스 연동
- 의미 기반 관련 기억 검색
- 대화 내용 자동 저장 및 인덱싱

### 4. LLM 통합
- 외부 LLM API와의 HTTP 통신
- 컨텍스트 기반 응답 생성
- 토큰 사용량 추적

## 채팅 처리 흐름

1. **세션 연결**: WebSocket을 통한 클라이언트 연결
2. **메시지 수신**: HTTP POST로 채팅 요청 처리
3. **컨텍스트 수집**: 
   - 벡터 메모리에서 관련 기억 검색
   - 최근 대화 기록 조회 (최대 10개)
4. **LLM 요청**: 컨텍스트와 함께 외부 LLM API 호출
5. **응답 생성**: LLM으로부터 응답 수신 및 처리
6. **실시간 전송**: WebSocket으로 클라이언트에 응답 전송
7. **기억 저장**: 대화 내용을 장기 기억에 저장

## 기술 스택

- **.NET 8.0**: 백엔드 프레임워크
- **ASP.NET Core**: 웹 API 및 WebSocket 지원
- **Windows Authentication**: 인증 시스템
- **Swagger**: API 문서화
- **Docker**: 컨테이너화 지원

## 설치 및 실행

### 필수 요구사항

- .NET 8.0 SDK
- 외부 서비스:
  - LLM API 서버 (포트 5601)
  - 벡터 메모리 저장소 (포트 5602)

### 실행 방법

1. **저장소 클론**
   ```bash
   git clone [repository-url]
   cd MainAPI\ Server
   ```

2. **의존성 복원**
   ```bash
   dotnet restore
   ```

3. **애플리케이션 실행**
   ```bash
   dotnet run
   ```

4. **Docker 실행** (선택사항)
   ```bash
   docker build -t mainapi-server .
   docker run -p 5000:5000 mainapi-server
   ```

### 환경 설정

`appsettings.json`에서 외부 서비스 URL 설정:

```json
{
  "ExternalServices": {
    "LLMApi": "http://localhost:5601",
    "MemoryApi": "http://localhost:5602"
  }
}
```

## API 명세

### HTTP API

#### POST `/api/chat`
채팅 메시지를 처리합니다.

**요청:**
```json
{
  "session_id": "session_123",
  "actor": "user",
  "message": "안녕하세요",
  "action": null
}
```

**응답:**
```json
{
  "session_id": "session_123",
  "response": "응답 메시지"
}
```

#### GET `/api/hello`
서버 상태를 확인합니다.

**응답:**
```json
{
  "status": "ok",
  "serverTime": "2024-01-01T00:00:00Z",
  "message": "MainAPIServer API is running."
}
```

### WebSocket API

#### WebSocket 연결: `/ws`
실시간 통신을 위한 WebSocket 연결

**연결 파라미터:**
- `sessionId` (선택사항): 기존 세션 ID

**연결 시 응답:**
```json
{
  "type": "session_id",
  "session_id": "session_123"
}
```

## 주요 컴포넌트

### ChatService
채팅 요청의 전체 처리 흐름을 관리하는 핵심 서비스입니다.

**주요 기능:**
- 메모리 검색 및 컨텍스트 수집
- LLM 요청 처리
- 대화 기록 저장
- 실시간 응답 전송

### SessionManager
WebSocket 세션을 관리하는 서비스입니다.

**주요 기능:**
- 세션 등록/해제
- 클라이언트별 메시지 전송
- 연결 상태 모니터링

### ConversationService
대화 기록을 관리하는 서비스입니다.

**주요 기능:**
- 세션별 대화 기록 저장
- 최근 메시지 조회
- 메시지 수 제한 관리

### WebSocketMiddleware
WebSocket 연결을 처리하는 미들웨어입니다.

**주요 기능:**
- WebSocket 요청 검증
- 세션 ID 생성 및 관리
- 연결 유지 및 정리


## 개발 가이드

### 새로운 기능 추가

1. **서비스 레이어**: `Services/` 폴더에 비즈니스 로직 구현
2. **컨트롤러**: `Controllers/` 폴더에 API 엔드포인트 추가
3. **모델**: `Models/` 폴더에 데이터 구조 정의
4. **의존성 주입**: `Program.cs`에서 서비스 등록

### 외부 서비스 연동

1. **인터페이스 정의**: `Clients/` 폴더에 인터페이스 생성
2. **구현체 작성**: HTTP 클라이언트 구현
3. **의존성 등록**: `Program.cs`에서 HttpClient 등록


## 지원

문제가 있거나 질문이 있으시면 [이슈 페이지](link-to-issues)를 통해 문의해 주세요.

---

**MainAPI Server** - 실시간 AI 챗봇 서버 