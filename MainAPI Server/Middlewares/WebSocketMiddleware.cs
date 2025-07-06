using MainAPI_Server.Services.Session;
using System.Net.WebSockets;

namespace MainAPI_Server.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws") {
                Console.WriteLine($"[WS] WebSocket 요청 수신");
                
                if (!context.WebSockets.IsWebSocketRequest) {
                    Console.WriteLine("[WS] WebSocket 요청이 아님");
                    if (!context.Response.HasStarted) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("WebSocket 요청이 아님");
                    }
                    return;
                }

                // 세션 ID 처리
                var sessionId = context.Request.Query["sessionId"];
                Console.WriteLine($"[WS] 쿼리에서 세션 ID: {sessionId}");
                
                // 세션 ID가 없으면 새로 생성
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = GenerateSessionId();
                    Console.WriteLine($"[WS] 새 세션 ID 생성: {sessionId}");
                }
                else
                {
                    Console.WriteLine($"[WS] 기존 세션 ID 사용: {sessionId}");
                }
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"[WS] WebSocket 수락됨, 상태: {socket.State}");

                SessionManager.Register(sessionId, socket);
                Console.WriteLine($"[WS] 세션 등록됨: {sessionId}");
                Console.WriteLine($"[WS] 활성 세션 수: {SessionManager.GetAll().Count()}");
                
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
                Console.WriteLine($"[WS] 클라이언트에게 세션 ID 전송됨: {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] 세션 ID 전송 실패: {ex.Message}");
            }
        }
        
        private async Task KeepWebSocketAlive(WebSocket socket, string sessionId)
        {
            var buffer = new byte[1024];
            try
            {
                Console.WriteLine($"[WS] 세션 {sessionId}의 WebSocket 연결 유지 시작");
                while (socket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"[WS] 세션 {sessionId}에서 메시지 대기 중, 소켓 상태: {socket.State}");
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[WS] 클라이언트 {sessionId}가 연결 종료 요청");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"[WS] 세션 {sessionId}에서 메시지 수신: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] 세션 {sessionId}의 WebSocket 연결 유지 중 오류: {ex.Message}");
                Console.WriteLine($"[WS] 예외 유형: {ex.GetType().Name}");
            }
            finally
            {
                Console.WriteLine($"[WS] 세션 {sessionId} 연결 해제됨, 소켓 상태: {socket.State}");
                SessionManager.Unregister(sessionId);
            }
        }
    }
}
