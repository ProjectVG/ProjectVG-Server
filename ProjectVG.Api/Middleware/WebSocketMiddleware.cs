using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Infrastructure.Realtime.WebSocketConnection;

namespace ProjectVG.Api.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly IConnectionRegistry _connectionRegistry;
        /// <summary>
        /// WebSocket 미들웨어의 인스턴스를 생성하고 필요한 의존성을 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 요청 파이프라인의 다음 미들웨어(delegate), 로거, 웹소켓 매니저 및 연결 레지스트리를 내부 필드에 할당합니다.
        /// </remarks>
        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IWebSocketManager webSocketService,
            IConnectionRegistry connectionRegistry)
        {
            _next = next;
            _logger = logger;
            _webSocketService = webSocketService;
            _connectionRegistry = connectionRegistry;
        }

        /// <summary>
        /// 지정된 HTTP 요청을 검사해 WebSocket 업그레이드 요청(/ws)을 처리하고 연결을 관리한다.
        /// </summary>
        /// <remarks>
        /// - 요청 경로가 "/ws"가 아니면 다음 미들웨어로 위임한다.
        /// - "/ws" 경로에서 WebSocket 업그레이드가 아니면 상태 코드 400을 반환하고 종료한다.
        /// - 쿼리 문자열의 sessionId를 사용하되 비어 있으면 고유한 세션 ID를 생성한다.
        /// - 동일한 세션 ID로 기존 연결이 등록되어 있으면 기존 연결을 WebSocket 매니저를 통해 정리(DisconnectAsync)한다.
        /// - 새로운 WebSocket 연결을 생성하여 연결 레지스트리에 등록(Register)하고, WebSocket 매니저의 ConnectAsync를 호출해 세션을 초기화한다.
        /// - 이후 HandleWebSocketConnection을 통해 수신되는 텍스트/바이너리 메시지를 전달하고, 연결 종료 시 WebSocket 매니저에 DisconnectAsync를 보장한다.
        /// - 부수적으로 HTTP 응답 상태 코드 및 등록 상태를 변경하며, 내부적으로 예외가 발생해도 정리 동작을 수행한다.
        /// </remarks>
        /// <returns>요청 처리가 완료되거나 WebSocket 연결의 수명주기 처리가 끝났을 때 완료되는 비동기 Task.</returns>
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
                var connection = new WebSocketClientConnection(actualSessionId, socket, userId: null);
                _connectionRegistry.Register(actualSessionId, connection);

                // 4. 세션 생성 (이제 연결이 등록된 상태)
                await _webSocketService.ConnectAsync(actualSessionId);

                await HandleWebSocketConnection(socket, actualSessionId);
                return;
            }

            await _next(context);
        }

        /// <summary>
        /// 지정된 WebSocket 연결에서 수신 루프를 실행하여 텍스트 메시지는 문자열로, 바이너리 메시지는 바이트 배열로 각각 처리자에 전달하고
        /// Close 메시지 또는 오류가 발생할 때까지 연결을 유지한다. 종료 시 항상 연결을 정리(DisconnectAsync)한다.
        /// </summary>
        /// <param name="socket">처리할 활성 WebSocket 인스턴스.</param>
        /// <param name="sessionId">메시지 전달 및 연결 식별에 사용되는 세션 식별자.</param>
        /// <returns>연결 처리가 완료될 때까지 완료되는 비동기 작업.</returns>
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
