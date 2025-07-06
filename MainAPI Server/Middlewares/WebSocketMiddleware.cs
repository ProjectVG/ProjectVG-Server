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
                Console.WriteLine($"[WS] WebSocket request received");
                
                if (!context.WebSockets.IsWebSocketRequest) {
                    Console.WriteLine("[WS] Not a WebSocket request");
                    if (!context.Response.HasStarted) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Not a WebSocket request");
                    }
                    return;
                }

                // 세션 ID 처리
                var sessionId = context.Request.Query["sessionId"];
                Console.WriteLine($"[WS] Session ID from query: {sessionId}");
                
                // 세션 ID가 없으면 새로 생성
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = GenerateSessionId();
                    Console.WriteLine($"[WS] Generated new session ID: {sessionId}");
                }
                else
                {
                    Console.WriteLine($"[WS] Using existing session ID: {sessionId}");
                }
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"[WS] WebSocket accepted, state: {socket.State}");

                SessionManager.Register(sessionId, socket);
                Console.WriteLine($"[WS] Session registered: {sessionId}");
                Console.WriteLine($"[WS] Total active sessions: {SessionManager.GetAll().Count()}");
                
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
                Console.WriteLine($"[WS] Session ID sent to client: {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Failed to send session ID: {ex.Message}");
            }
        }
        
        private async Task KeepWebSocketAlive(WebSocket socket, string sessionId)
        {
            var buffer = new byte[1024];
            try
            {
                Console.WriteLine($"[WS] Starting KeepWebSocketAlive for session: {sessionId}");
                while (socket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"[WS] Waiting for message from {sessionId}, socket state: {socket.State}");
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"[WS] Client {sessionId} requested close");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"[WS] Received message from {sessionId}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Error in KeepWebSocketAlive for {sessionId}: {ex.Message}");
                Console.WriteLine($"[WS] Exception type: {ex.GetType().Name}");
            }
            finally
            {
                Console.WriteLine($"[WS] Session {sessionId} disconnected, socket state: {socket.State}");
                SessionManager.Unregister(sessionId);
            }
        }
    }
}
