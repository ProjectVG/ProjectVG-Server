using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Session;

namespace ProjectVG.Application.Middlewares
{
    public class StatelessWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StatelessWebSocketMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StatelessWebSocketMiddleware(RequestDelegate next, ILogger<StatelessWebSocketMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws")
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("WebSocket 요청이 아님");
                    return;
                }

                var sessionId = context.Request.Query["sessionId"];
                if (string.IsNullOrEmpty(sessionId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("세션 ID가 필요합니다");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var clientSessionService = scope.ServiceProvider.GetRequiredService<IClientSessionService>();

                // 세션 유효성 검사
                var isValid = await clientSessionService.ValidateSessionAsync(sessionId);
                if (!isValid)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("유효하지 않은 세션 ID입니다");
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                
                // 클라이언트 IP와 Port 가져오기
                var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var clientPort = context.Connection.RemotePort;

                // 세션에 클라이언트 정보 업데이트
                var session = await clientSessionService.GetSessionAsync(sessionId);
                if (session != null)
                {
                    session.ClientIP = clientIP;
                    session.ClientPort = clientPort;
                    await clientSessionService.UpdateSessionAsync(session);
                    
                    _logger.LogInformation("Stateless 연결 설정 완료: {SessionId} -> {ClientIP}:{ClientPort}", 
                        sessionId, clientIP, clientPort);
                }

                // 성공 메시지 전송 후 즉시 연결 종료
                await SendSuccessMessage(socket, sessionId);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "연결 설정 완료", CancellationToken.None);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task SendSuccessMessage(WebSocket socket, string sessionId)
        {
            try
            {
                var message = $"{{\"type\":\"connection_success\",\"session_id\":\"{sessionId}\",\"message\":\"UDP 전송 준비 완료\"}}";
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
                _logger.LogError(ex, "성공 메시지 전송 실패: {SessionId}", sessionId);
            }
        }
    }
} 