using ProjectVG.Application.Services.Session;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Realtime.WebSocketConnection;
using System.Net.WebSockets;

namespace ProjectVG.Api.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly IJwtProvider _jwtProvider;

        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            ISessionManager sessionManager,
            IWebSocketConnectionManager connectionManager,
            IJwtProvider jwtProvider)
        {
            _next = next;
            _logger = logger;
            _sessionManager = sessionManager;
            _connectionManager = connectionManager;
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
        /// 새 아키텍처: 세션 관리와 WebSocket 연결 관리 분리
        /// </summary>
        private async Task RegisterConnection(Guid userId, WebSocket socket)
        {
            var userIdString = userId.ToString();
            _logger.LogInformation("[WebSocketMiddleware] 연결 등록 시작: UserId={UserId}", userId);

            try
            {
                // 기존 로컬 연결이 있으면 정리
                if (_connectionManager.HasLocalConnection(userIdString))
                {
                    _logger.LogInformation("[WebSocketMiddleware] 기존 로컬 연결 발견 - 정리 중: UserId={UserId}", userId);
                    _connectionManager.UnregisterConnection(userIdString);
                }

                // 1. 세션 관리자에 세션 생성 (Redis 저장)
                await _sessionManager.CreateSessionAsync(userId);
                _logger.LogInformation("[WebSocketMiddleware] 세션 관리자에 세션 저장 완료: UserId={UserId}", userId);

                // 2. WebSocket 연결 관리자에 로컬 연결 등록
                var connection = new WebSocketClientConnection(userIdString, socket);
                _connectionManager.RegisterConnection(userIdString, connection);
                _logger.LogInformation("[WebSocketMiddleware] 로컬 WebSocket 연결 등록 완료: UserId={UserId}", userId);

                // [디버그] 등록 후 상태 확인
                var isSessionActive = await _sessionManager.IsSessionActiveAsync(userId);
                var hasLocalConnection = _connectionManager.HasLocalConnection(userIdString);
                _logger.LogInformation("[WebSocketMiddleware] 연결 등록 완료: UserId={UserId}, SessionActive={SessionActive}, LocalConnection={LocalConnection}",
                    userId, isSessionActive, hasLocalConnection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketMiddleware] 연결 등록 실패: UserId={UserId}", userId);
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

                            // 세션 하트비트 업데이트 (Redis TTL 갱신)
                            try {
                                if (Guid.TryParse(userId, out var userGuid))
                                {
                                    await _sessionManager.UpdateSessionHeartbeatAsync(userGuid);
                                }
                            }
                            catch (Exception heartbeatEx) {
                                _logger.LogWarning(heartbeatEx, "세션 하트비트 업데이트 실패: {UserId}", userId);
                            }
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
                    // 새 아키텍처: 세션과 로컬 연결 분리해서 정리
                    if (Guid.TryParse(userId, out var userGuid))
                    {
                        // 1. 세션 관리자에서 세션 삭제 (Redis에서 제거)
                        await _sessionManager.DeleteSessionAsync(userGuid);
                        _logger.LogDebug("세션 관리자에서 세션 삭제 완료: {UserId}", userId);
                    }

                    // 2. 로컬 WebSocket 연결 해제
                    _connectionManager.UnregisterConnection(userId);
                    _logger.LogDebug("로컬 WebSocket 연결 해제 완료: {UserId}", userId);

                    // 3. WebSocket 소켓 정리
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived) {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed",
                            CancellationToken.None);
                        _logger.LogDebug("WebSocket 소켓 정리 완료: {UserId}", userId);
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "WebSocket 정리 중 오류: {UserId}", userId);
                }
                finally {
                    cancellationTokenSource?.Dispose();
                }
            }
        }
    }
}
