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
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IJwtProvider _jwtProvider;

        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IWebSocketManager webSocketService,
            IConnectionRegistry connectionRegistry,
            IJwtProvider jwtProvider)
        {
            _next = next;
            _logger = logger;
            _webSocketService = webSocketService;
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
        /// 기존 연결 정리 후 새 연결 등록 
        /// </summary>
        private async Task RegisterConnection(Guid userId, WebSocket socket)
        {
            if (_connectionRegistry.TryGet(userId.ToString(), out var existing) && existing != null) {
                _logger.LogInformation("기존 연결 정리: {UserId}", userId);
                await _webSocketService.DisconnectAsync(userId.ToString());
            }

            var connection = new WebSocketClientConnection(userId.ToString(), socket);
            _connectionRegistry.Register(userId.ToString(), connection);
            await _webSocketService.ConnectAsync(userId.ToString());
        }

        /// <summary> 
        /// 세션 루프 실행 
        /// </summary>
        private async Task RunSessionLoop(WebSocket socket, string userId)
        {
            var buffer = new byte[1024];
            try {
                while (socket.State == WebSocketState.Open) {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        _logger.LogInformation("연결 종료 요청: {UserId}", userId);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "세션 루프 오류: {UserId}", userId);
            }
            finally {
                _logger.LogInformation("연결 해제: {UserId}", userId);
                await _webSocketService.DisconnectAsync(userId);
            }
        }
    }
}
