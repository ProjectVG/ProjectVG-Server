# ProjectVG Chat 시스템 및 WebSocket 세션 관리 흐름

## 개요

이 문서는 ProjectVG API 서버의 Chat 시스템과 분산 WebSocket 세션 관리의 전체 흐름을 상세히 설명합니다. HTTP Chat 요청 처리부터 분산 환경에서의 실시간 메시지 전달까지의 전체 과정을 다룹니다.

## 1. Chat 로직 실행 흐름

### 📌 HTTP Chat 요청 흐름

```
Unity Client ──HTTP POST──→ ChatController ──→ ChatService ──→ Background Pipeline
     │                      /api/v1/chat         EnqueueChatRequestAsync
     │                                                    │
     └──WebSocket 연결──────────────────────────────────┘
                                                          │
                                           ┌─────────────▼─────────────┐
                                           │   Chat Processing Pipeline │
                                           │  (Background Task)         │
                                           └─────────────┬─────────────┘
                                                         │
                                              ┌─────────▼─────────┐
                                              │  결과를 WebSocket  │
                                              │  으로 실시간 전송  │
                                              └───────────────────┘
```

### 📋 Chat Processing Pipeline 상세

```csharp
// 1. ChatController.ProcessChat (ProjectVG.Api/Controllers/ChatController.cs:22)
[HttpPost]
[JwtAuthentication]
public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var command = new ChatRequestCommand(userGuid, request.CharacterId, request.Message, request.RequestAt, request.UseTTS);

    // 2. 즉시 응답 후 백그라운드 처리
    var result = await _chatService.EnqueueChatRequestAsync(command);
    return Ok(result);
}

// 3. ChatService Background Pipeline (7단계)
private async Task ProcessChatRequestInternalAsync(ChatProcessContext context)
{
    // 1. 검증 → ChatRequestValidator
    // 2. 전처리 → UserInputAnalysisProcessor, MemoryContextPreprocessor
    // 3. 액션 처리 → UserInputActionProcessor
    // 4. LLM 처리 → ChatLLMProcessor (Cost Tracking 데코레이터)
    // 5. TTS 처리 → ChatTTSProcessor (Cost Tracking 데코레이터)
    // 6. 결과 처리 → ChatResultProcessor
    // 7. 성공/실패 → ChatSuccessHandler/ChatFailureHandler
}
```

## 2. WebSocket 연결 과정 분석

### 🔌 WebSocket 연결 흐름

```
Unity Client ──WebSocket(/ws)──→ WebSocketMiddleware ──→ JWT 검증 ──→ 연결 등록
     │                           InvokeAsync:31                        │
     │                                                                 │
     └──Query Parameter: ?token={jwt} 또는 Authorization Header─────────┘
                                     │
                            ┌───────▼───────┐
                            │ 1. JWT 검증   │
                            │ 2. 기존 연결   │
                            │    정리       │
                            │ 3. 새 연결    │
                            │    등록       │
                            │ 4. 세션 루프   │
                            │    시작       │
                            └───────────────┘
```

### 📋 WebSocket 연결 과정 상세

```csharp
// 1. WebSocketMiddleware.InvokeAsync (ProjectVG.Api/Middleware/WebSocketMiddleware.cs:31)
public async Task InvokeAsync(HttpContext context)
{
    if (context.Request.Path != "/ws") {
        await _next(context);
        return;
    }

    // 2. JWT 토큰 검증 (Line 44)
    var userId = ValidateAndExtractUserId(context);
    if (userId == null) {
        context.Response.StatusCode = 401;
        return;
    }

    // 3. WebSocket 연결 수락 (Line 50)
    var socket = await context.WebSockets.AcceptWebSocketAsync();

    // 4. 연결 등록 (Line 51)
    await RegisterConnection(userId.Value, socket);

    // 5. 세션 루프 시작 (Line 52)
    await RunSessionLoop(socket, userId.Value.ToString());
}

// 6. 연결 등록 과정 (Line 94)
private async Task RegisterConnection(Guid userId, WebSocket socket)
{
    // 기존 연결 정리
    if (_connectionRegistry.TryGet(userId.ToString(), out var existing) && existing != null) {
        await _webSocketService.DisconnectAsync(userId.ToString());
    }

    // 새 연결 등록
    var connection = new WebSocketClientConnection(userId.ToString(), socket);
    _connectionRegistry.Register(userId.ToString(), connection);  // 로컬 레지스트리
    await _webSocketService.ConnectAsync(userId.ToString());     // 분산 세션 관리
}
```

## 3. 세션 저장 및 사용 추적

### 🗃️ 분산 세션 관리 시스템

```
         로컬 메모리                     Redis (분산 저장소)
    ┌─────────────────┐              ┌─────────────────────────┐
    │ ConnectionRegistry│             │ session:user:{userId}   │
    │ ConcurrentDict   │◄──────────► │ {                       │
    │ [userId] = conn  │             │   "ConnectionId": "...", │
    │                  │             │   "ServerId": "api-001", │
    └─────────────────┘             │   "ConnectedAt": "...",  │
                                     │   "LastActivity": "..." │
                                     │ }                       │
                                     │                         │
                                     │ user:server:{userId}    │
                                     │ = "api-server-001"      │
                                     └─────────────────────────┘
```

### 📋 세션 저장 과정

```csharp
// 1. DistributedWebSocketManager.ConnectAsync (ProjectVG.Application/Services/WebSocket/DistributedWebSocketManager.cs:35)
public async Task<string> ConnectAsync(string userId)
{
    // 2. Redis에 세션 정보 저장 (Line 39)
    await _sessionStorage.CreateAsync(new SessionInfo
    {
        SessionId = userId,
        UserId = userId,
        ConnectedAt = DateTime.UtcNow
    });

    // 3. 분산 브로커 채널 구독 (Line 47)
    if (_distributedBroker != null)
    {
        await _distributedBroker.SubscribeToUserChannelAsync(userId);
    }
}

// 4. DistributedMessageBroker.SubscribeToUserChannelAsync (Line 148)
public async Task SubscribeToUserChannelAsync(string userId)
{
    // Redis 채널 구독
    var userChannel = $"user:{userId}";
    await _subscriber.SubscribeAsync(userChannel, OnUserMessageReceived);

    // 사용자-서버 매핑 설정
    await _serverRegistration.SetUserServerAsync(userId, _serverId);
}
```

### 🔍 세션 사용 시점

1. **연결 시**: ConnectionRegistry (로컬) + Redis 세션 저장
2. **메시지 전송 시**: Redis에서 사용자가 어느 서버에 있는지 조회
3. **연결 해제 시**: 로컬 레지스트리 + Redis 세션 정리

## 4. 분산환경에서의 Chat 도메인 흐름

### 🌐 분산 Chat 메시지 전달 흐름

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Unity Client   │    │  API Server A   │    │  API Server B   │
│                 │    │  (요청 처리)     │    │  (사용자 연결)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
          │                       │                       │
          │ 1. HTTP Chat 요청     │                       │
          ├──────────────────────►│                       │
          │                       │                       │
          │ 2. 즉시 응답 (202)    │                       │
          │◄──────────────────────┤                       │
          │                       │                       │
          │                       │ 3. LLM 서비스 호출    │
          │                       ├─────────────────────► │
          │                       │                    외부 서비스
          │                       │ 4. Chat 응답 받음     │
          │                       │◄─────────────────────┤
          │                       │                       │
          │                       │ 5. Redis Pub/Sub      │
          │                       │    user:{userId}      │
          │                       ├──────────────────────►│
          │                       │                    Redis
          │                       │                       │
          │                       │                       │ 6. Redis Sub 수신
          │                       │                       │◄────────────────
          │                       │                       │
          │ 7. WebSocket 실시간   │                       │
          │    결과 전송          │                       │
          │◄──────────────────────┼───────────────────────┤
          │                       │                       │
```

### 📋 분산 Chat 처리 상세

```csharp
// 1. Server A에서 Chat 요청 처리
// ChatService.EnqueueChatRequestAsync → Background Pipeline 실행

// 2. Pipeline 완료 후 결과 전송
// ChatSuccessHandler에서 분산 메시지 전송
await _distributedWebSocketManager.SendToUserAsync(userId, chatResult);

// 3. DistributedMessageBroker.SendToUserAsync (Line 66)
public async Task SendToUserAsync(string userId, object message)
{
    // 로컬에 사용자가 있는지 확인
    var isLocalActive = _webSocketManager.IsSessionActive(userId);

    if (isLocalActive) {
        // 로컬 직접 전송
        await SendLocalMessage(userId, message);
        return;
    }

    // 사용자가 어느 서버에 있는지 Redis에서 조회
    var targetServerId = await _serverRegistration.GetUserServerAsync(userId);

    if (string.IsNullOrEmpty(targetServerId)) {
        _logger.LogWarning("사용자가 연결된 서버를 찾을 수 없음: {UserId}", userId);
        return;
    }

    // 해당 서버로 Redis Pub/Sub 메시지 전송
    var brokerMessage = BrokerMessage.CreateUserMessage(userId, message, _serverId);
    var userChannel = $"user:{userId}";
    await _subscriber.PublishAsync(userChannel, brokerMessage.ToJson());
}

// 4. Server B에서 Redis 메시지 수신
// DistributedMessageBroker.OnUserMessageReceived (Line 187)
private async void OnUserMessageReceived(RedisChannel channel, RedisValue message)
{
    var brokerMessage = BrokerMessage.FromJson(message!);

    // 로컬에서 해당 사용자가 연결되어 있는지 확인
    var isLocalActive = _webSocketManager.IsSessionActive(brokerMessage.TargetUserId);

    if (isLocalActive) {
        var payload = brokerMessage.DeserializePayload<object>();
        await SendLocalMessage(brokerMessage.TargetUserId, payload);
    }
}
```

## Redis 키 구조

### 🔑 핵심 Redis 키 구조

```redis
# 사용자 세션 정보
session:user:12345 = {
  "ConnectionId": "conn_abc123",
  "ServerId": "api-server-001",
  "ConnectedAt": "2024-01-15T10:30:00Z",
  "LastActivity": "2024-01-15T10:45:00Z"
}

# 사용자-서버 매핑
user:server:12345 = "api-server-001"

# 서버 등록 정보
server:api-server-001 = {
  "ServerId": "api-server-001",
  "StartedAt": "2024-01-15T10:00:00Z",
  "LastHeartbeat": "2024-01-15T10:45:30Z",
  "ActiveConnections": 25,
  "Status": "healthy"
}

# 활성 서버 목록
servers:active = {"api-server-001", "api-server-002", "api-server-003"}

# Redis Pub/Sub 채널
user:12345          # 특정 사용자 메시지
server:api-001      # 특정 서버 메시지
broadcast           # 전체 브로드캐스트
```

## 핵심 컴포넌트

### 주요 클래스 및 파일

| 컴포넌트 | 파일 위치 | 역할 |
|----------|-----------|------|
| `ChatController` | `ProjectVG.Api/Controllers/ChatController.cs:22` | HTTP Chat 요청 접수 |
| `ChatService` | `ProjectVG.Application/Services/Chat/ChatService.cs` | Chat 처리 파이프라인 orchestration |
| `WebSocketMiddleware` | `ProjectVG.Api/Middleware/WebSocketMiddleware.cs:31` | WebSocket 연결 관리 |
| `DistributedWebSocketManager` | `ProjectVG.Application/Services/WebSocket/DistributedWebSocketManager.cs:35` | 분산 WebSocket 세션 관리 |
| `DistributedMessageBroker` | `ProjectVG.Application/Services/MessageBroker/DistributedMessageBroker.cs:66` | Redis Pub/Sub 메시지 브로커 |
| `ConnectionRegistry` | `ProjectVG.Application/Services/Session/ConnectionRegistry.cs` | 로컬 연결 레지스트리 |

### Chat Processing Pipeline 단계

1. **ChatRequestValidator**: 입력 검증
2. **UserInputAnalysisProcessor**: 사용자 의도 분석
3. **MemoryContextPreprocessor**: 메모리 컨텍스트 수집
4. **UserInputActionProcessor**: 액션 처리
5. **ChatLLMProcessor**: LLM 호출 (Cost Tracking 적용)
6. **ChatTTSProcessor**: TTS 처리 (Cost Tracking 적용)
7. **ChatResultProcessor**: 결과 처리
8. **ChatSuccessHandler/ChatFailureHandler**: 성공/실패 처리

## 💡 핵심 요약

1. **Chat 요청**: HTTP로 즉시 응답 → 백그라운드 파이프라인 → WebSocket으로 결과 전송
2. **WebSocket 연결**: JWT 검증 → 로컬 레지스트리 등록 → Redis 세션 저장 → 채널 구독
3. **세션 관리**: 로컬(ConnectionRegistry) + 분산(Redis) 이중 저장
4. **분산 메시지**: Redis Pub/Sub로 서버 간 실시간 메시지 라우팅
5. **서버 발견**: Redis 기반 서버 등록/헬스체크/정리 시스템

이 시스템을 통해 여러 API 서버 인스턴스가 실행되어도 사용자는 어느 서버에서든 Chat 요청을 보낼 수 있고, WebSocket으로 실시간 결과를 받을 수 있습니다.

## 관련 문서

- [분산 시스템 개요](../distributed-system/README.md)
- [WebSocket 연결 관리](./websocket_connection_management.md)
- [WebSocket HTTP 아키텍처](./websocket_http_architecture.md)
- [REST API 엔드포인트](../api/rest-endpoints.md)