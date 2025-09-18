# ProjectVG API Server - 핵심 기능 상세

이 문서는 ProjectVG API Server의 핵심 기능별 상세 구현과 코드 위치를 다룹니다.

## 📋 목차

- [1. 채팅 시스템](#1-채팅-시스템)
- [2. 인증 시스템](#2-인증-시스템)
- [3. 크레딧 시스템](#3-크레딧-시스템)
- [4. AI 캐릭터 관리](#4-ai-캐릭터-관리)

---

## 1. 채팅 시스템

### HTTP-WebSocket Bridge 패턴: 비동기 장시간 작업 처리

**설명**: HTTP 요청으로 채팅을 시작하고 WebSocket으로 결과를 전송하는 비동기 아키텍처. 장시간 소요되는 LLM 처리를 논블로킹 방식으로 구현하여 클라이언트 타임아웃을 방지합니다.

**아키텍처 플로우**:
```
Client ──HTTP POST──→ ChatController ──Enqueue──→ Background Processing
   ↑                                                      ↓
   └──WebSocket Push────← WebSocketManager ←──Result────┘
```

**구현 위치**:
- **HTTP 진입점**: [`ProjectVG.Api/Controllers/ChatController.cs`](../../ProjectVG.Api/Controllers/ChatController.cs)
- **WebSocket 결과 전송**: [`ProjectVG.Application/Services/WebSocket/WebSocketManager.cs`](../../ProjectVG.Application/Services/WebSocket/WebSocketManager.cs)
- **비동기 처리 오케스트레이션**: [`ProjectVG.Application/Services/Chat/ChatService.cs`](../../ProjectVG.Application/Services/Chat/ChatService.cs)

**핵심 코드**:
```csharp
// HTTP로 요청 접수 (즉시 응답)
[HttpPost]
[JwtAuthentication]
public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
{
    var command = new ChatRequestCommand(userId, request.CharacterId, request.Message);

    // 백그라운드 처리 시작 (논블로킹)
    var result = await _chatService.EnqueueChatRequestAsync(command);

    // 즉시 Accepted 응답 반환
    return Ok(new { status = "ACCEPTED", sessionId = result.SessionId });
}

// 백그라운드에서 비동기 처리 후 WebSocket으로 결과 전송
public async Task<ChatRequestResult> EnqueueChatRequestAsync(ChatRequestCommand command)
{
    await _validator.ValidateAsync(command);
    var context = await PrepareChatRequestAsync(command);

    // 백그라운드 태스크로 장시간 작업 실행
    _ = Task.Run(async () => await ProcessChatRequestInternalAsync(context));

    return ChatRequestResult.Accepted(command.Id.ToString(), command.UserId, command.CharacterId);
}
```

### ChatService 오케스트레이션 패턴: 복합 서비스 조율

**설명**: ChatService는 Facade 패턴을 구현하여 채팅 처리에 필요한 여러 서비스들을 단일 인터페이스로 추상화합니다. 비용 추적 데코레이터를 통해 LLM과 TTS 비용을 자동으로 추적합니다.

**실제 처리 플로우**:
```
1. 전처리 단계:
   - Validation: ChatRequestValidator
   - Input Analysis: UserInputAnalysisProcessor (+ Cost Tracking)
   - Action Processing: UserInputActionProcessor
   - Memory Context: MemoryContextPreprocessor
   - Character/Conversation Info

2. 백그라운드 처리:
   - LLM Processing: ChatLLMProcessor (+ Cost Tracking)
   - TTS Processing: ChatTTSProcessor (+ Cost Tracking)
   - Success Handling: ChatSuccessHandler
   - Result Persistence: ChatResultProcessor
```

**구현 위치**:
- **메인 오케스트레이터**: [`ProjectVG.Application/Services/Chat/ChatService.cs`](../../ProjectVG.Application/Services/Chat/ChatService.cs)
- **비용 추적 데코레이터**: [`ProjectVG.Application/Services/Chat/CostTracking/`](../../ProjectVG.Application/Services/Chat/CostTracking/)

**핵심 코드**:
```csharp
// Facade 패턴으로 채팅 처리 추상화
public async Task<ChatRequestResult> EnqueueChatRequestAsync(ChatRequestCommand command)
{
    // 1. 즉시 검증
    await _validator.ValidateAsync(command);

    // 2. 전처리 (context 준비)
    var preprocessContext = await PrepareChatRequestAsync(command);

    // 3. 백그라운드에서 비동기 처리
    _ = Task.Run(async () => {
        await ProcessChatRequestInternalAsync(preprocessContext);
    });

    // 4. 즉시 Accepted 응답
    return ChatRequestResult.Accepted(command.Id.ToString(), command.UserId, command.CharacterId);
}

// 실제 백그라운드 처리
private async Task ProcessChatRequestInternalAsync(ChatProcessContext context)
{
    try {
        await _llmProcessor.ProcessAsync(context);  // 비용 추적 있음
        await _ttsProcessor.ProcessAsync(context);  // 비용 추적 있음

        var successHandler = scope.ServiceProvider.GetRequiredService<ChatSuccessHandler>();
        var resultProcessor = scope.ServiceProvider.GetRequiredService<ChatResultProcessor>();

        await successHandler.HandleAsync(context);
        await resultProcessor.PersistResultsAsync(context);
    }
    catch (Exception) {
        var failureHandler = scope.ServiceProvider.GetRequiredService<ChatFailureHandler>();
        await failureHandler.HandleAsync(context);
    }
    finally {
        _metricsService.EndChatMetrics();
    }
}
```

### WebSocket 세션 관리: 실시간 결과 전송

**설명**: WebSocket을 통한 실시간 채팅 결과 전송 시스템. 클라이언트가 `/ws` 엔드포인트로 연결하면 채팅 처리 완료 시 결과를 자동으로 수신합니다.

**구현 위치**:
- **WebSocket 미들웨어**: [`ProjectVG.Api/Middleware/WebSocketMiddleware.cs`](../../ProjectVG.Api/Middleware/WebSocketMiddleware.cs)
- **WebSocket 매니저**: [`ProjectVG.Application/Services/WebSocket/WebSocketManager.cs`](../../ProjectVG.Application/Services/WebSocket/WebSocketManager.cs)
- **연결 관리**: [`ProjectVG.Infrastructure/Realtime/WebSocketConnection/WebSocketClientConnection.cs`](../../ProjectVG.Infrastructure/Realtime/WebSocketConnection/WebSocketClientConnection.cs)

**핵심 코드**:
```csharp
// WebSocket 연결 처리 (미들웨어)
if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
{
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var userId = await AuthenticateWebSocketAsync(context);

    if (userId.HasValue)
    {
        await _webSocketManager.AddConnectionAsync(userId.Value, webSocket);
        await HandleWebSocketCommunication(webSocket, userId.Value);
    }
}

// 채팅 결과 WebSocket으로 전송
public async Task SendChatResultAsync(Guid userId, ChatResult result)
{
    var connection = _connections.GetValueOrDefault(userId);
    if (connection != null)
    {
        var message = JsonSerializer.Serialize(new WebSocketMessage
        {
            Type = "chat_result",
            Data = result
        });

        await connection.SendAsync(message);
    }
}
```

---

## 2. 인증 시스템

### JWT 토큰: Access (15분) + Refresh (30일) 이중 토큰

**설명**: 단기 Access Token과 장기 Refresh Token을 활용한 안전한 인증 시스템

**구현 위치**:
- **JWT 생성**: [`ProjectVG.Infrastructure/Auth/JwtProvider.cs`](../../ProjectVG.Infrastructure/Auth/JwtProvider.cs)
- **토큰 서비스**: [`ProjectVG.Infrastructure/Auth/TokenService.cs`](../../ProjectVG.Infrastructure/Auth/TokenService.cs)
- **설정**: `appsettings.json` JWT 섹션

**핵심 코드**:
```csharp
// Access Token 생성 (15분)
public string GenerateAccessToken(Guid userId)
{
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
    return CreateToken(claims, _accessTokenLifetime);
}

// Refresh Token 생성 (30일)
public string GenerateRefreshToken(Guid userId)
{
    var claims = new[] {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim("token_type", "refresh")
    };
    return CreateToken(claims, _refreshTokenLifetime);
}
```

### OAuth2 PKCE: Google 인증, 게스트 로그인

**설명**: Proof Key for Code Exchange를 활용한 안전한 OAuth2 인증과 게스트 로그인 지원

**구현 위치**:
- **OAuth2 컨트롤러**: [`ProjectVG.Api/Controllers/OAuthController.cs`](../../ProjectVG.Api/Controllers/OAuthController.cs)
- **Google Provider**: [`ProjectVG.Application/Services/Auth/Providers/GoogleOAuth2Provider.cs`](../../ProjectVG.Application/Services/Auth/Providers/GoogleOAuth2Provider.cs)
- **게스트 로그인**: [`ProjectVG.Api/Controllers/AuthController.cs`](../../ProjectVG.Api/Controllers/AuthController.cs)

**핵심 코드**:
```csharp
// OAuth2 PKCE 인증 시작
[HttpGet("authorize/{provider}")]
public async Task<IActionResult> Authorize(string provider, [FromQuery] string state = "", [FromQuery] string codeChallenge = "")
{
    var authUrl = await _authService.GenerateOAuth2AuthorizeUrlAsync(provider, state, codeChallenge);
    return Ok(new { authUrl });
}

// 게스트 로그인
[HttpPost("guest-login")]
public async Task<IActionResult> GuestLogin([FromBody] string guestId)
{
    var result = await _authService.GuestLoginAsync(guestId);
    return Ok(new { success = true, tokens = result.Tokens, user = result.User });
}
```

### Redis 세션: 토큰 관리 및 blacklist 지원

**설명**: Redis를 활용한 분산 세션 관리와 토큰 blacklist 기능

**구현 위치**:
- **Redis 토큰 저장소**: [`ProjectVG.Infrastructure/Auth/RedisRefreshTokenStorage.cs`](../../ProjectVG.Infrastructure/Auth/RedisRefreshTokenStorage.cs)
- **메모리 fallback**: [`ProjectVG.Infrastructure/Auth/InMemoryRefreshTokenStorage.cs`](../../ProjectVG.Infrastructure/Auth/InMemoryRefreshTokenStorage.cs)
- **서비스 등록**: [`ProjectVG.Infrastructure/InfrastructureServiceCollectionExtensions.cs`](../../ProjectVG.Infrastructure/InfrastructureServiceCollectionExtensions.cs)

**핵심 코드**:
```csharp
// Redis에 Refresh Token 저장
public async Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt)
{
    var key = $"refresh_token:{refreshToken}";
    var data = new RefreshTokenData { UserId = userId, ExpiresAt = expiresAt };
    var expiry = expiresAt - DateTime.UtcNow;

    await _database.StringSetAsync(key, JsonSerializer.Serialize(data), expiry);
    return true;
}

// 토큰 검증 및 blacklist 확인
public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
{
    var key = $"refresh_token:{refreshToken}";
    return await _database.KeyExistsAsync(key);
}
```

### 다중 헤더: Authorization, X-Access-Credit, X-Refresh-Credit

**설명**: 다양한 인프라 환경을 고려한 다중 헤더 지원 인증 필터

**구현 위치**:
- **JWT 인증 필터**: [`ProjectVG.Api/Filters/JwtAuthenticationFilter.cs`](../../ProjectVG.Api/Filters/JwtAuthenticationFilter.cs)

**핵심 코드**:
```csharp
private string? ExtractToken(HttpRequest request)
{
    var possibleHeaders = new[]
    {
        "Authorization",           // 표준 Bearer 토큰
        "X-Access-Credit",         // 커스텀 Access 토큰 헤더
        "X-Refresh-Credit",        // 커스텀 Refresh 토큰 헤더
        "X-Forwarded-Authorization", // API Gateway 환경
        "X-Original-Authorization",  // Load Balancer 환경
        "HTTP_AUTHORIZATION"         // CGI 환경
    };

    foreach (var headerName in possibleHeaders) {
        var headerValue = request.Headers[headerName].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer ")) {
            return headerValue.Substring("Bearer ".Length).Trim();
        }
    }
    return null;
}
```

---

## 3. 크레딧 시스템

### 정밀 계산: Decimal(18,2) 잔액 관리

**설명**: 금융급 정밀도의 Decimal(18,2) 타입을 사용한 크레딧 잔액 관리

**구현 위치**:
- **사용자 엔티티**: [`ProjectVG.Domain/Entities/User/User.cs`](../../ProjectVG.Domain/Entities/User/User.cs)
- **크레딧 거래 엔티티**: [`ProjectVG.Domain/Entities/Credit/CreditTransaction.cs`](../../ProjectVG.Domain/Entities/Credit/CreditTransaction.cs)
- **크레딧 서비스**: [`ProjectVG.Application/Services/Credit/CreditService.cs`](../../ProjectVG.Application/Services/Credit/CreditService.cs)

**핵심 코드**:
```csharp
// 사용자 엔티티의 크레딧 필드
[Precision(18, 2)]
public decimal CreditBalance { get; set; } = 0;

[Precision(18, 2)]
public decimal TotalCreditsEarned { get; set; } = 0;

[Precision(18, 2)]
public decimal TotalCreditsSpent { get; set; } = 0;

// 크레딧 거래 엔티티
public class CreditTransaction : BaseEntity
{
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    public CreditTransactionType Type { get; set; }  // Earn = 1, Spend = 2
}

// 크레딧 계산 로직
public async Task<decimal> AddCreditsAsync(Guid userId, decimal amount, string source, string description)
{
    var user = await _userRepository.GetByIdAsync(userId);
    var newBalance = user.CreditBalance + amount;

    user.CreditBalance = newBalance;
    user.TotalCreditsEarned += amount;

    return newBalance;
}
```

### 거래 기록: 완전한 audit trail

**설명**: 모든 크레딧 변동 사항을 추적 가능한 거래 기록으로 관리

**구현 위치**:
- **크레딧 거래 엔티티**: [`ProjectVG.Domain/Entities/Credit/CreditTransaction.cs`](../../ProjectVG.Domain/Entities/Credit/CreditTransaction.cs)
- **거래 리포지토리**: [`ProjectVG.Infrastructure/Persistence/Repositories/Credit/SqlServerCreditTransactionRepository.cs`](../../ProjectVG.Infrastructure/Persistence/Repositories/Credit/SqlServerCreditTransactionRepository.cs)

**핵심 코드**:
```csharp
public class CreditTransaction : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;  // 중복 방지용 고유 ID

    [Required]
    public CreditTransactionType Type { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }  // 거래 후 잔액 스냅샷

    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;  // "CHAT_USAGE", "INITIAL_BONUS" 등

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string? RelatedEntityType { get; set; }  // "Character", "Conversation" 등

    [StringLength(100)]
    public string? RelatedEntityId { get; set; }
}

// 거래 기록 생성
private async Task<CreditTransaction> CreateTransactionAsync(
    Guid userId, string transactionId, CreditTransactionType type,
    decimal amount, decimal balanceAfter, string source, string description)
{
    var transaction = new CreditTransaction
    {
        UserId = userId,
        TransactionId = transactionId,
        Type = type,
        Amount = amount,
        BalanceAfter = balanceAfter,
        Source = source,
        Description = description
    };

    return await _creditTransactionRepository.CreateAsync(transaction);
}
```

### 동시성 제어: Optimistic concurrency 지원

**설명**: RowVersion을 활용한 낙관적 동시성 제어로 크레딧 잔액 일관성 보장

**구현 위치**:
- **사용자 엔티티**: [`ProjectVG.Domain/Entities/User/User.cs`](../../ProjectVG.Domain/Entities/User/User.cs)
- **사용자 리포지토리**: [`ProjectVG.Infrastructure/Persistence/Repositories/User/SqlServerUserRepository.cs`](../../ProjectVG.Infrastructure/Persistence/Repositories/User/SqlServerUserRepository.cs)

**핵심 코드**:
```csharp
// 사용자 엔티티의 동시성 제어
public class User : BaseEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[0];

    [Precision(18, 2)]
    public decimal CreditBalance { get; set; } = 0;
}

// 동시성 충돌 처리
public async Task<User> UpdateAsync(User user)
{
    try
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // 동시성 충돌 발생 시 재시도 로직
        _logger.LogWarning("동시성 충돌 발생 - 사용자: {UserId}", user.Id);
        throw new ConcurrencyException("다른 사용자가 동시에 수정했습니다. 다시 시도해주세요.", ex);
    }
}

// 크레딧 차감 시 동시성 안전 처리
public async Task<decimal> SpendCreditsAsync(Guid userId, decimal amount, string source, string description)
{
    const int maxRetries = 3;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user.CreditBalance < amount)
                throw new InsufficientCreditsException();

            user.CreditBalance -= amount;
            user.TotalCreditsSpent += amount;

            await _userRepository.UpdateAsync(user);  // RowVersion 검사

            // 거래 기록 생성
            await CreateTransactionAsync(userId, Guid.NewGuid().ToString(),
                CreditTransactionType.Spend, amount, user.CreditBalance, source, description);

            return user.CreditBalance;
        }
        catch (ConcurrencyException)
        {
            retryCount++;
            if (retryCount >= maxRetries) throw;

            await Task.Delay(100 * retryCount);  // 지수 백오프
        }
    }

    throw new ConcurrencyException("동시성 충돌로 인해 처리할 수 없습니다.");
}
```

---

## 4. AI 캐릭터 관리

### 하이브리드 설정: JSON 필드 구성 + 직접 프롬프트 입력

**설명**: 구조화된 JSON 설정과 자유형 SystemPrompt를 모두 지원하는 유연한 캐릭터 구성 시스템

**구현 위치**:
- **캐릭터 엔티티**: [`ProjectVG.Domain/Entities/Character/Character.cs`](../../ProjectVG.Domain/Entities/Character/Character.cs)
- **개별 설정**: [`ProjectVG.Domain/Entities/Character/IndividualConfig.cs`](../../ProjectVG.Domain/Entities/Character/IndividualConfig.cs)
- **설정 모드**: [`ProjectVG.Domain/Entities/Character/CharacterConfigMode.cs`](../../ProjectVG.Domain/Entities/Character/CharacterConfigMode.cs)

**핵심 코드**:
```csharp
public enum CharacterConfigMode { Individual = 0, SystemPrompt = 1 }

// JSON 설정과 C# 객체 간 자동 변환
[NotMapped]
public IndividualConfig? IndividualConfig
{
    get => string.IsNullOrEmpty(IndividualConfigJson)
        ? null
        : JsonSerializer.Deserialize<IndividualConfig>(IndividualConfigJson);
    set => IndividualConfigJson = value == null
        ? null
        : JsonSerializer.Serialize(value);
}

// 모드에 따른 효과적인 SystemPrompt 반환
public string GetEffectiveSystemPrompt()
{
    return ConfigMode switch
    {
        CharacterConfigMode.SystemPrompt => SystemPrompt ?? string.Empty,
        CharacterConfigMode.Individual => IndividualConfig?.BuildSystemPrompt() ?? string.Empty,
        _ => string.Empty
    };
}
```

### 소유권 모델: 시스템/공개/개인 캐릭터 권한 관리

**설명**: 시스템 캐릭터, 공개 캐릭터, 개인 캐릭터의 3단계 소유권 및 접근 권한 관리

**구현 위치**:
- **캐릭터 엔티티**: [`ProjectVG.Domain/Entities/Character/Character.cs`](../../ProjectVG.Domain/Entities/Character/Character.cs)
- **캐릭터 서비스**: [`ProjectVG.Application/Services/Character/CharacterService.cs`](../../ProjectVG.Application/Services/Character/CharacterService.cs)
- **캐릭터 컨트롤러**: [`ProjectVG.Api/Controllers/CharacterController.cs`](../../ProjectVG.Api/Controllers/CharacterController.cs)

**핵심 코드**:
```csharp
// 시스템 캐릭터 확인 (UserId가 null)
public bool IsSystemCharacter() => UserId == null;

// 소유자 확인
public bool IsOwnedBy(Guid userId) => UserId.HasValue && UserId.Value == userId;

// 접근 권한 확인
public bool CanBeViewedBy(Guid? userId)
{
    // 공개 캐릭터는 누구나 볼 수 있음
    if (IsPublic) return true;

    // 비공개 캐릭터는 소유자만 볼 수 있음
    return userId.HasValue && IsOwnedBy(userId.Value);
}

// 편집 권한 확인
public bool CanBeEditedBy(Guid? userId)
{
    // 시스템 캐릭터는 편집 불가
    if (IsSystemCharacter()) return false;

    // 소유자만 편집 가능
    return userId.HasValue && IsOwnedBy(userId.Value);
}
```

### 프롬프트 생성: 설정 기반 SystemPrompt 구성

**설명**: IndividualConfig의 구조화된 필드들로부터 동적 SystemPrompt 생성

**구현 위치**:
- **개별 설정**: [`ProjectVG.Domain/Entities/Character/IndividualConfig.cs`](../../ProjectVG.Domain/Entities/Character/IndividualConfig.cs)

**핵심 코드**:
```csharp
public string BuildSystemPrompt()
{
    var prompt = new StringBuilder();

    // 기본 설정
    if (!string.IsNullOrEmpty(Name))
        prompt.AppendLine($"당신의 이름은 {Name}입니다.");

    if (!string.IsNullOrEmpty(Personality))
        prompt.AppendLine($"성격: {Personality}");

    if (!string.IsNullOrEmpty(SpeechStyle))
        prompt.AppendLine($"말하는 방식: {SpeechStyle}");

    if (!string.IsNullOrEmpty(Background))
        prompt.AppendLine($"배경: {Background}");

    // 관계 설정
    if (!string.IsNullOrEmpty(RelationshipWithUser))
        prompt.AppendLine($"사용자와의 관계: {RelationshipWithUser}");

    // 행동 가이드라인
    if (BehaviorGuidelines?.Any() == true)
    {
        prompt.AppendLine("행동 가이드라인:");
        foreach (var guideline in BehaviorGuidelines)
            prompt.AppendLine($"- {guideline}");
    }

    return prompt.ToString().Trim();
}
```

---

## 📚 추가 리소스

- **API 문서**: [README.md](../../README.md)
- **아키텍처 가이드**: [CLAUDE.md](../../CLAUDE.md)
- **개발 환경 설정**: [scripts/](../../scripts/) 디렉토리
- **테스트 가이드**: [ProjectVG.Tests/](../../ProjectVG.Tests/) 디렉토리