# ProjectVG - 멀티 모듈 아키텍처 AI 챗봇 서버

실시간 AI 챗봇 서버로, Clean Architecture와 멀티 모듈 아키텍처를 적용하여 WebSocket과 HTTP API를 통해 클라이언트와 통신하며 LLM(Large Language Model)과 벡터 메모리 저장소를 연동하는 시스템입니다.

## 프로젝트 개요

ProjectVG는 개인화된 AI 어시스턴트를 구현한 실시간 챗봇 서버입니다. Clean Architecture 원칙을 따라 도메인 중심의 설계로 구성되어 있으며, 사용자와의 대화를 기억하고, 컨텍스트를 고려한 맞춤형 응답을 제공합니다.

### 핵심 특징
- **Clean Architecture**: 도메인 중심의 계층화된 아키텍처
- **멀티 모듈 구조**: 관심사 분리를 통한 모듈화
- **실시간 WebSocket 통신**: 양방향 실시간 통신
- **벡터 기반 장기 기억 시스템**: 의미 기반 메모리 검색
- **세션별 대화 기록 관리**: 개인화된 대화 컨텍스트
- **개인화된 AI 페르소나 지원**: 캐릭터 기반 AI 응답

## 시스템 아키텍처

```
Client ↔ WebSocket/HTTP API ↔ ProjectVG.Api ↔ ProjectVG.Application ↔ ProjectVG.Domain
                                    ↓
                            ┌─────────────┬─────────────┐
                            │   LLM API   │ Memory API  │
                            │ (Port 5601) │ (Port 5602) │
                            └─────────────┴─────────────┘
```

### Clean Architecture 레이어

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ProjectVG.Api (Controllers, Middlewares)                 │
├─────────────────────────────────────────────────────────────┤
│                   Application Layer                        │
│  ProjectVG.Application (Services, DTOs, Commands)         │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer                            │
│  ProjectVG.Domain (Entities, Enums, Common)               │
├─────────────────────────────────────────────────────────────┤
│                 Infrastructure Layer                        │
│  ProjectVG.Infrastructure (Repositories, External APIs)   │
├─────────────────────────────────────────────────────────────┤
│                   Common Layer                             │
│  ProjectVG.Common (Constants, Exceptions, Extensions)     │
└─────────────────────────────────────────────────────────────┘
```

## 프로젝트 구조

```
ProjectVG/
├── ProjectVG.Api/                    # 프레젠테이션 레이어
│   ├── Controllers/                  # API 엔드포인트
│   │   ├── ChatController.cs         # 채팅 요청 처리
│   │   ├── CharacterController.cs    # 캐릭터 관리
│   │   └── HealthController.cs       # 헬스체크
│   ├── Middlewares/                  # 미들웨어
│   │   └── WebSocketMiddleware.cs    # WebSocket 처리
│   ├── Models/                       # API 모델
│   │   ├── Character/                # 캐릭터 관련 모델
│   │   └── Chat/                     # 채팅 관련 모델
│   └── Program.cs                    # 애플리케이션 진입점
│
├── ProjectVG.Application/             # 애플리케이션 레이어
│   ├── Services/                     # 비즈니스 로직 서비스
│   │   ├── Chat/                     # 채팅 서비스
│   │   ├── Character/                # 캐릭터 서비스
│   │   ├── Conversation/             # 대화 관리 서비스
│   │   ├── LLM/                      # LLM 연동 서비스
│   │   └── Session/                  # 세션 관리 서비스
│   ├── Models/                       # DTO 및 Command 모델
│   │   ├── Character/                # 캐릭터 DTO/Command
│   │   ├── Chat/                     # 채팅 DTO/Command
│   │   └── User/                     # 사용자 DTO/Command
│   ├── Middlewares/                  # 애플리케이션 미들웨어
│   └── DTOs/                        # 데이터 전송 객체
│
├── ProjectVG.Domain/                 # 도메인 레이어
│   ├── Entities/                     # 도메인 엔티티
│   │   ├── Character/                # 캐릭터 엔티티
│   │   ├── Chat/                     # 채팅 엔티티
│   │   ├── ConversationHistory/      # 대화 기록 엔티티
│   │   └── User/                     # 사용자 엔티티
│   ├── Enums/                        # 도메인 열거형
│   │   └── ChatRole.cs               # 채팅 역할 정의
│   └── Common/                       # 도메인 공통 요소
│       └── BaseEntity.cs             # 기본 엔티티 클래스
│
├── ProjectVG.Infrastructure/         # 인프라스트럭처 레이어
│   ├── ExternalApis/                 # 외부 API 클라이언트
│   │   ├── LLM/                      # LLM API 클라이언트
│   │   └── MemoryClient/             # 메모리 저장소 클라이언트
│   ├── Repositories/                 # 데이터 접근 계층
│   │   ├── InMemory/                 # 인메모리 저장소 구현
│   │   ├── ICharacterRepository.cs   # 캐릭터 저장소 인터페이스
│   │   ├── IConversationRepository.cs # 대화 저장소 인터페이스
│   │   ├── ISessionRepository.cs     # 세션 저장소 인터페이스
│   │   └── IUserRepository.cs        # 사용자 저장소 인터페이스
│   └── Services/                     # 인프라 서비스
│       └── Session/                  # 세션 관련 서비스
│
└── ProjectVG.Common/                 # 공통 레이어
    ├── Constants/                    # 상수 정의
    │   ├── AppConstants.cs           # 애플리케이션 상수
    │   ├── ErrorCodes.cs             # 에러 코드
    │   └── LLMSettings.cs            # LLM 설정 상수
    ├── Exceptions/                   # 커스텀 예외
    │   ├── ProjectVGException.cs     # 기본 예외 클래스
    │   ├── NotFoundException.cs       # 리소스 없음 예외
    │   ├── ValidationException.cs     # 검증 예외
    │   └── ExternalServiceException.cs # 외부 서비스 예외
    ├── Extensions/                   # 확장 메서드
    │   └── ExceptionExtensions.cs    # 예외 확장 메서드
    └── Configuration/                # 설정 클래스
        └── LLMSettings.cs            # LLM 설정
```

## 모듈별 역할

### ProjectVG.Api (프레젠테이션 레이어)
- **역할**: HTTP API 엔드포인트와 WebSocket 연결 처리
- **주요 구성요소**:
  - Controllers: REST API 엔드포인트
  - Middlewares: WebSocket 및 인증 미들웨어
  - Models: API 요청/응답 모델

### ProjectVG.Application (애플리케이션 레이어)
- **역할**: 비즈니스 로직 처리 및 도메인 서비스 조율
- **주요 구성요소**:
  - Services: 비즈니스 로직 서비스
  - Models: DTO, Command, Query 모델
  - Middlewares: 애플리케이션 미들웨어

### ProjectVG.Domain (도메인 레이어)
- **역할**: 핵심 비즈니스 엔티티와 도메인 로직
- **주요 구성요소**:
  - Entities: 도메인 엔티티 (Character, ChatMessage, User 등)
  - Enums: 도메인 열거형
  - Common: 공통 도메인 요소

### ProjectVG.Infrastructure (인프라스트럭처 레이어)
- **역할**: 외부 서비스 연동 및 데이터 접근
- **주요 구성요소**:
  - ExternalApis: 외부 API 클라이언트 (LLM, Memory)
  - Repositories: 데이터 접근 구현체
  - Services: 인프라 서비스

### ProjectVG.Common (공통 레이어)
- **역할**: 모든 레이어에서 공통으로 사용되는 요소
- **주요 구성요소**:
  - Constants: 애플리케이션 상수
  - Exceptions: 커스텀 예외 클래스
  - Extensions: 확장 메서드
  - Configuration: 설정 클래스

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

### 5. 캐릭터 관리
- AI 페르소나 정의 및 관리
- 캐릭터별 개성과 배경 설정
- 동적 메타데이터 지원

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
- **Clean Architecture**: 도메인 중심 아키텍처
- **Dependency Injection**: 의존성 주입
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
   cd ProjectVG
   ```

2. **의존성 복원**
   ```bash
   dotnet restore
   ```

3. **애플리케이션 실행**
   ```bash
   dotnet run --project ProjectVG.Api
   ```

4. **Docker 실행** (선택사항)
   ```bash
   docker build -t projectvg-api .
   docker run -p 5000:5000 projectvg-api
   ```

### 환경 설정

`ProjectVG.Api/appsettings.json`에서 외부 서비스 URL 설정:

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

#### GET `/api/characters`
모든 캐릭터를 조회합니다.

**응답:**
```json
[
  {
    "id": "guid",
    "name": "캐릭터명",
    "description": "설명",
    "role": "역할",
    "personality": "성격",
    "background": "배경",
    "isActive": true,
    "metadata": {}
  }
]
```

#### POST `/api/characters`
새 캐릭터를 생성합니다.

**요청:**
```json
{
  "name": "캐릭터명",
  "description": "설명",
  "role": "역할",
  "personality": "성격",
  "background": "배경",
  "metadata": {}
}
```

#### GET `/api/hello`
서버 상태를 확인합니다.

**응답:**
```json
{
  "status": "ok",
  "serverTime": "2024-01-01T00:00:00Z",
  "message": "ProjectVG API is running."
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

### ChatService (Application Layer)
채팅 요청의 전체 처리 흐름을 관리하는 핵심 서비스입니다.

**주요 기능:**
- 메모리 검색 및 컨텍스트 수집
- LLM 요청 처리
- 대화 기록 저장
- 실시간 응답 전송

### CharacterService (Application Layer)
캐릭터 관리 비즈니스 로직을 처리하는 서비스입니다.

**주요 기능:**
- 캐릭터 CRUD 작업
- 캐릭터 검증 및 비즈니스 규칙 적용
- 캐릭터 메타데이터 관리

### SessionService (Application Layer)
WebSocket 세션을 관리하는 서비스입니다.

**주요 기능:**
- 세션 등록/해제
- 클라이언트별 메시지 전송
- 연결 상태 모니터링

### ConversationService (Application Layer)
대화 기록을 관리하는 서비스입니다.

**주요 기능:**
- 세션별 대화 기록 저장
- 최근 메시지 조회
- 메시지 수 제한 관리

### WebSocketMiddleware (Api Layer)
WebSocket 연결을 처리하는 미들웨어입니다.

**주요 기능:**
- WebSocket 요청 검증
- 세션 ID 생성 및 관리
- 연결 유지 및 정리


## 지원

문제가 있거나 질문이 있으시면 [이슈 페이지](link-to-issues)를 통해 문의해 주세요.

---
