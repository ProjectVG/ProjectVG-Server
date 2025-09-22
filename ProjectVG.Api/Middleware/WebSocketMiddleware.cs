using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Realtime.WebSocketConnection;
using System.Net.WebSockets;

namespace ProjectVG.Api.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly IDistributedWebSocketManager _distributedWebSocketManager;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IJwtProvider _jwtProvider;

        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IWebSocketManager webSocketService,
            IDistributedWebSocketManager distributedWebSocketManager,
            IConnectionRegistry connectionRegistry,
            IJwtProvider jwtProvider)
        {
            _next = next;
            _logger = logger;
            _webSocketService = webSocketService;
            _distributedWebSocketManager = distributedWebSocketManager;
            _connectionRegistry = connectionRegistry;
            _jwtProvider = jwtProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != "/ws") {
                await _next(context);
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest) {
                _logger.LogWarning("WebSocket 요청이 아님");
                context.Response.StatusCode = 400;
                return;
            }

            var userId = ValidateAndExtractUserId(context);
            if (userId == null) {
                context.Response.StatusCode = 401;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await RegisterConnection(userId.Value, socket);
            await RunSessionLoop(socket, userId.Value.ToString());
        }

        /// <summary> 
        /// JWT 토큰 검증 및 사용자 ID 추출 
        /// </summary>
        private Guid? ValidateAndExtractUserId(HttpContext context)
        {
            var token = ExtractToken(context);

            if (string.IsNullOrEmpty(token)) {
                _logger.LogWarning("JWT 토큰 없음");
                return null;
            }

            var userIdString = _jwtProvider.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId)) {
                _logger.LogWarning("JWT 토큰이 유효하지 않음");
                return null;
            }

            return userId;
        }

        /// <summary> 
        /// QueryString 또는 Authorization 헤더에서 토큰 추출 
        /// </summary>
        private string ExtractToken(HttpContext context)
        {
            var token = context.Request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token)) return token;

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                return authHeader.Substring("Bearer ".Length).Trim();

            return string.Empty;
        }

        /// <summary>
        /// 기존 연결 정리 후 새 연결 등록 (분산 세션 관리 포함)
        /// </summary>
        private async Task RegisterConnection(Guid userId, WebSocket socket)
        {
            var userIdString = userId.ToString();

            try
            {
                // 1. 기존 로컬 연결 정리
                if (_connectionRegistry.TryGet(userIdString, out var existing) && existing != null) {
                    _logger.LogInformation("기존 로컬 연결 정리: {UserId}", userId);
                    await _webSocketService.DisconnectAsync(userIdString);
                }

                // 2. 기존 분산 세션 정리
                if (await _distributedWebSocketManager.IsSessionActiveAsync(userIdString))
                {
                    _logger.LogInformation("기존 분산 세션 정리: {UserId}", userId);
                    await _distributedWebSocketManager.DisconnectAsync(userIdString);
                }

                // 3. 새 로컬 연결 등록
                var connection = new WebSocketClientConnection(userIdString, socket);
                _connectionRegistry.Register(userIdString, connection);

                // 4. 로컬 WebSocket 서비스에 연결
                await _webSocketService.ConnectAsync(userIdString);

                // 5. 분산 세션 관리자에 등록 (표준화된 서버 ID 사용)
                var serverId = GenerateServerId();
                await _distributedWebSocketManager.ConnectAsync(userIdString, serverId);

                _logger.LogInformation("WebSocket 연결 등록 완료: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket 연결 등록 실패: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 세션 루프 실행
        /// </summary>
        private async Task RunSessionLoop(WebSocket socket, string userId)
        {
            var buffer = new byte[1024 * 4]; // Increase buffer size for better performance
            var cancellationTokenSource = new CancellationTokenSource();

            // Set a reasonable timeout for WebSocket operations
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(30));

            try {
                _logger.LogInformation("WebSocket 세션 시작: {UserId}", userId);

                // Send initial connection confirmation without exposing user ID
                var welcomeMessage = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"connected\",\"status\":\"success\"}");
                await socket.SendAsync(
                    new ArraySegment<byte>(welcomeMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancellationTokenSource.Token);

                while (socket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    using var ms = new MemoryStream();
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("연결 종료 요청: {UserId}", userId);
                            break;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    // WebSocket의 기본 제어 메시지들 처리
                    if (result.MessageType == WebSocketMessageType.Binary) {
                        _logger.LogDebug("Binary 메시지 받음: {UserId}", userId);
                        continue;
                    }

                    // Handle heartbeat/ping messages
                    if (result.MessageType == WebSocketMessageType.Text) {
                        var message = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                        // 매우 단순한 ping 판별 → 추후 JSON 파싱으로 교체 권장
                        if (string.Equals(message, "ping", StringComparison.OrdinalIgnoreCase) ||
                            message.Contains("\"type\":\"ping\"", StringComparison.OrdinalIgnoreCase)) {
                            var pongMessage = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"pong\"}");
                            await socket.SendAsync(
                                new ArraySegment<byte>(pongMessage),
                                WebSocketMessageType.Text,
                                true,
                                cancellationTokenSource.Token);
                        }
                    }
                }
            }
            catch (OperationCanceledException) {
                _logger.LogWarning("WebSocket 세션 타임아웃: {UserId}", userId);
            }
            catch (WebSocketException ex) {
                _logger.LogWarning(ex, "WebSocket 연결 오류: {UserId}", userId);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "세션 루프 예상치 못한 오류: {UserId}", userId);
            }
            finally {
                _logger.LogInformation("WebSocket 연결 해제: {UserId}", userId);

                try {
                    // 1. 로컬 WebSocket 서비스에서 연결 해제
                    await _webSocketService.DisconnectAsync(userId);

                    // 2. 로컬 연결 레지스트리에서 해제
                    _connectionRegistry.Unregister(userId);

                    // 3. 분산 세션 관리자에서 해제
                    await _distributedWebSocketManager.DisconnectAsync(userId);

                    // 4. WebSocket 연결 정리
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived) {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed",
                            CancellationToken.None);
                    }

                    _logger.LogInformation("WebSocket 연결 정리 완료: {UserId}", userId);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "WebSocket 정리 중 오류: {UserId}", userId);
                }
                finally {
                    cancellationTokenSource?.Dispose();
                }
            }
        }

        /// <summary>
        /// 표준화된 서버 ID 생성
        /// </summary>
        private static string GenerateServerId()
        {
            // 1. 환경변수에서 서버 ID 조회 (최우선)
            var envServerId = Environment.GetEnvironmentVariable("SERVER_ID");
            if (!string.IsNullOrWhiteSpace(envServerId))
            {
                return envServerId.Trim();
            }

            // 2. 표준화된 형식으로 자동 생성
            var machineName = Environment.MachineName.ToLowerInvariant();
            var processId = Environment.ProcessId;
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmm");

            return $"api-server-{machineName}-{processId}-{timestamp}";
        }
    }
}
