using ProjectVG.Application.Services;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Session;

namespace ProjectVG.Application.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // WebSocket 경로 확인 (/ws 또는 루트 경로)
            if (context.Request.Path == "/ws" || context.Request.Path == "/")
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

                // 세션 ID 처리
                var sessionId = context.Request.Query["sessionId"];
                var isNewSession = string.IsNullOrEmpty(sessionId);
                
                if (isNewSession)
                {
                    sessionId = GenerateSessionId();
                    _logger.LogInformation("새 WebSocket 세션 생성: {SessionId}", sessionId);
                }
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                
                // 서비스 로케이터를 사용하여 scoped 서비스 해결
                using var scope = _serviceProvider.CreateScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                await sessionService.RegisterSessionAsync(sessionId);
                
                // 클라이언트에게 세션 ID 전송
                await SendSessionIdToClient(socket, sessionId);

                // WebSocket 연결 유지
                await KeepWebSocketAlive(socket, sessionId, sessionService);

                return; 
            }

            await _next(context);
        }

        private string GenerateSessionId()
        {
            return $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
        }
        
        private async Task SendSessionIdToClient(WebSocket socket, string sessionId)
        {
            try
            {
                var message = $"{{\"type\":\"session_id\",\"session_id\":\"{sessionId}\"}}";
                var buffer = System.Text.Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 ID 전송 실패: {SessionId}", sessionId);
            }
        }
        
        private async Task KeepWebSocketAlive(WebSocket socket, string sessionId, ISessionService sessionService)
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
                await sessionService.UnregisterSessionAsync(sessionId);
            }
        }
    }
} 