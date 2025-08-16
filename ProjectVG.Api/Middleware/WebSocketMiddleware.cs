using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Api.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IClientConnectionFactory _connectionFactory;

        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IWebSocketManager webSocketService,
            IConnectionRegistry connectionRegistry,
            IClientConnectionFactory connectionFactory)
        {
            _next = next;
            _logger = logger;
            _webSocketService = webSocketService;
            _connectionRegistry = connectionRegistry;
            _connectionFactory = connectionFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws") {
                if (!context.WebSockets.IsWebSocketRequest) {
                    _logger.LogWarning("WebSocket 요청이 아님");
                    context.Response.StatusCode = 400;
                    return;
                }

                var sessionId = context.Request.Query["sessionId"].ToString();
                var socket = await context.WebSockets.AcceptWebSocketAsync();

                // 1. 세션 ID 생성 (연결 등록 없이)
                var actualSessionId = string.IsNullOrWhiteSpace(sessionId) ?
                    $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}" : sessionId;

                // 2. 기존 연결이 있다면 정리
                if (_connectionRegistry.TryGet(actualSessionId, out var existingConnection) && existingConnection != null) {
                    _logger.LogInformation("기존 연결을 정리합니다: {SessionId}", actualSessionId);
                    await _webSocketService.DisconnectAsync(actualSessionId);
                }

                // 3. 연결 생성 및 등록
                var connection = _connectionFactory.Create(actualSessionId, socket, userId: null);
                _connectionRegistry.Register(actualSessionId, connection);

                // 4. 세션 생성 (이제 연결이 등록된 상태)
                await _webSocketService.ConnectAsync(actualSessionId);

                await HandleWebSocketConnection(socket, actualSessionId);
                return;
            }

            await _next(context);
        }

        private async Task HandleWebSocketConnection(WebSocket socket, string sessionId)
        {
            var buffer = new byte[1024];
            try {
                while (socket.State == WebSocketState.Open) {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        _logger.LogInformation("WebSocket 연결 종료 요청: {SessionId}", sessionId);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text) {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await _webSocketService.HandleMessageAsync(sessionId, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary) {
                        var data = new byte[result.Count];
                        Array.Copy(buffer, data, result.Count);
                        await _webSocketService.HandleBinaryMessageAsync(sessionId, data);
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "WebSocket 연결 유지 중 오류: {SessionId}", sessionId);
            }
            finally {
                _logger.LogInformation("WebSocket 연결 해제: {SessionId}", sessionId);
                await _webSocketService.DisconnectAsync(sessionId);
            }
        }
    }
}
