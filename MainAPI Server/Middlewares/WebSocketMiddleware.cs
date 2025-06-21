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

                // 세션 등록
                var sessionId = context.Request.Query["sessionId"];
                Console.WriteLine($"[WS] Session ID from query: {sessionId}");
                
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"[WS] WebSocket accepted, state: {socket.State}");

                SessionManager.Register(sessionId, socket);
                Console.WriteLine($"[WS] Session registered: {sessionId}");

                // WebSocket 연결 유지
                await KeepWebSocketAlive(socket, sessionId);

                return; 
            }

            await _next(context);
        }

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
                        Console.WriteLine($"[WS] Client {sessionId} requested close");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS] Error in KeepWebSocketAlive for {sessionId}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[WS] Session {sessionId} disconnected");
                SessionManager.Unregister(sessionId);
            }
        }
    }
}
