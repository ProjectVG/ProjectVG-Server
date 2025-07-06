using MainAPI_Server.Services.Session;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace MainAPI_Server.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly ISessionManager _sessionManager;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger, ISessionManager sessionManager)
        {
            _next = next;
            _logger = logger;
            _sessionManager = sessionManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws") {
                _logger.LogInformation("WebSocket 요청 수신");
                
                if (!context.WebSockets.IsWebSocketRequest) {
                    _logger.LogWarning("WebSocket 요청이 아님");
                    if (!context.Response.HasStarted) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("WebSocket 요청이 아님");
                    }
                    return;
                }

                // 세션 ID 처리
                var sessionId = context.Request.Query["sessionId"];
                _logger.LogDebug("쿼리에서 세션 ID: {SessionId}", sessionId);
                
                // 세션 ID가 없으면 새로 생성
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = GenerateSessionId();
                    _logger.LogInformation("새 세션 ID 생성: {SessionId}", sessionId);
                }
                else
                {
                    _logger.LogInformation("기존 세션 ID 사용: {SessionId}", sessionId);
                }
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                _logger.LogDebug("WebSocket 수락됨, 상태: {SocketState}", socket.State);

                _sessionManager.Register(sessionId, socket);
                _logger.LogInformation("세션 등록됨: {SessionId}", sessionId);
                _logger.LogDebug("활성 세션 수: {ActiveSessionCount}", _sessionManager.GetAll().Count());
                
                // 클라이언트에게 세션 ID 전송
                await SendSessionIdToClient(socket, sessionId);

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
                _logger.LogDebug("클라이언트에게 세션 ID 전송됨: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 ID 전송 실패: {SessionId}", sessionId);
            }
        }
        
        private async Task KeepWebSocketAlive(WebSocket socket, string sessionId)
        {
            var buffer = new byte[1024];
            try
            {
                _logger.LogInformation("세션 {SessionId}의 WebSocket 연결 유지 시작", sessionId);
                while (socket.State == WebSocketState.Open)
                {
                    _logger.LogTrace("세션 {SessionId}에서 메시지 대기 중, 소켓 상태: {SocketState}", sessionId, socket.State);
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("클라이언트 {SessionId}가 연결 종료 요청", sessionId);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("세션 {SessionId}에서 메시지 수신: {Message}", sessionId, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 {SessionId}의 WebSocket 연결 유지 중 오류", sessionId);
            }
            finally
            {
                _logger.LogInformation("세션 {SessionId} 연결 해제됨, 소켓 상태: {SocketState}", sessionId, socket.State);
                _sessionManager.Unregister(sessionId);
            }
        }
    }
}
