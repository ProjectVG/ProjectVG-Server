using MainAPI_Server.Services.Session;

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
                if (!context.WebSockets.IsWebSocketRequest) {
                    if (!context.Response.HasStarted) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Not a WebSocket request");
                    }
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                return; 
            }

            await _next(context);
        }
    }
}
