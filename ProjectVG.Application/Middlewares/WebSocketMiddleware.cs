using ProjectVG.Application.Services;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Persistence.Session;

namespace ProjectVG.Application.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IClientConnectionFactory _connectionFactory;
        private readonly ISessionStorage _sessionStorage;

        /// <summary>
        /// WebSocket 미들웨어를 초기화합니다
        /// </summary>
        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IConnectionRegistry connectionRegistry,
            IClientConnectionFactory connectionFactory,
            ISessionStorage sessionStorage)
        {
            _next = next;
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _connectionFactory = connectionFactory;
            _sessionStorage = sessionStorage;
        }

        /// <summary>
        /// WebSocket 요청을 처리합니다
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws")
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    _logger.LogWarning("WebSocket 요청이 아님");
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("WebSocket 요청이 아님");
                    }
                    return;
                }

                // 세션 ID 처리 (항상 순수 문자열로 강제)
                var raw = context.Request.Query["sessionId"];
                var sessionId = string.IsNullOrWhiteSpace(raw) ? GenerateSessionId() : raw.ToString();
                _logger.LogInformation("새 WebSocket 세션 생성: {SessionId}", sessionId);
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();

                var connection = _connectionFactory.Create(sessionId, socket, userId: null);
                _connectionRegistry.Register(sessionId, connection);

                await _sessionStorage.CreateAsync(new SessionInfo
                {
                    SessionId = sessionId,
                    UserId = null,
                    ConnectedAt = DateTime.UtcNow
                });

                var sessionData = new WebSocketMessage("session", new { session_id = sessionId });
                var json = System.Text.Json.JsonSerializer.Serialize(sessionData);
                await _connectionRegistry.SendTextAsync(sessionId, json);

                // WebSocket 연결 유지
                await KeepWebSocketAlive(socket, sessionId);

                return; 
            }

            await _next(context);
        }

        private string GenerateSessionId()
        {
            return $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
        }
        
        /// <summary>
        /// WebSocket 연결을 유지하고 종료 이벤트를 처리합니다
        /// </summary>
        private async Task KeepWebSocketAlive(WebSocket socket, string sessionId)
        {
            var buffer = new byte[1024];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket 연결 종료 요청: {SessionId}", sessionId);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("WebSocket 메시지 수신: {SessionId} - {Message}", sessionId, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket 연결 유지 중 오류: {SessionId}", sessionId);
            }
            finally
            {
                _logger.LogInformation("WebSocket 연결 해제: {SessionId}", sessionId);
                _connectionRegistry.Unregister(sessionId);
            }
        }
    }
} 