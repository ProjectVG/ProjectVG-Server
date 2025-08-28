using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Realtime.WebSocketConnection;
using System.Net.WebSockets;

namespace ProjectVG.Api.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IJwtProvider _jwtProvider;

        /// <summary>
        /// WebSocket 미들웨어의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 요청 파이프라인 델리게이트, 로거, 웹소켓 서비스, 연결 레지스트리 및 JWT 제공자를 주입받아 내부 필드에 저장합니다.
        /// </remarks>
        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            IWebSocketManager webSocketService,
            IConnectionRegistry connectionRegistry,
            IJwtProvider jwtProvider)
        {
            _next = next;
            _logger = logger;
            _webSocketService = webSocketService;
            _connectionRegistry = connectionRegistry;
            _jwtProvider = jwtProvider;
        }

        /// <summary>
        /// "/ws" 경로로 들어온 WebSocket 업그레이드 요청을 처리하고 인증된 사용자의 연결을 등록한 뒤 세션 루프를 실행합니다.
        /// </summary>
        /// <param name="context">현재 HTTP 요청/응답 컨텍스트. WebSocket 업그레이드 요청이어야 하며 토큰은 쿼리 문자열 또는 Authorization 헤더의 Bearer 토큰에서 추출됩니다.</param>
        /// <returns>미들웨어 처리가 완료될 때까지 완료되지 않는 비동기 작업. (연결 수명 주기 동안 실행됩니다.)</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path != "/ws") {
                await _next(context);
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest) {
                _logger.LogWarning("WebSocket 요청이 아님");
                context.Response.StatusCode = 400;
                return;
            }

            var userId = ValidateAndExtractUserId(context);
            if (userId == null) {
                context.Response.StatusCode = 401;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await RegisterConnection(userId.Value, socket);
            await RunSessionLoop(socket, userId.Value.ToString());
        }

        /// <summary> 
        /// JWT 토큰 검증 및 사용자 ID 추출 
        /// <summary>
        /// 요청의 JWT를 검증하고 토큰에서 사용자 ID를 추출하여 Guid로 반환합니다.
        /// </summary>
        /// <param name="context">토큰을 포함할 수 있는 요청 컨텍스트(HttpContext).</param>
        /// <returns>
        /// 토큰이 존재하고 유효하면 해당 사용자 ID(Guid). 그렇지 않으면 null을 반환합니다.
        /// </returns>
        private Guid? ValidateAndExtractUserId(HttpContext context)
        {
            var token = ExtractToken(context);

            if (string.IsNullOrEmpty(token)) {
                _logger.LogWarning("JWT 토큰 없음");
                return null;
            }

            var userIdString = _jwtProvider.GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId)) {
                _logger.LogWarning("JWT 토큰이 유효하지 않음");
                return null;
            }

            return userId;
        }

        /// <summary> 
        /// QueryString 또는 Authorization 헤더에서 토큰 추출 
        /// <summary>
        /// HttpContext에서 JWT 토큰을 추출합니다.
        /// </summary>
        /// <remarks>
        /// 먼저 쿼리 문자열의 "token" 매개변수를 확인하고, 존재하지 않으면 Authorization 헤더의 "Bearer {token}" 부분을 사용합니다.
        /// 토큰이 없으면 빈 문자열을 반환합니다.
        /// </remarks>
        /// <param name="context">요청 정보가 포함된 HttpContext.</param>
        /// <returns>발견된 토큰 문자열 또는 토큰이 없으면 빈 문자열.</returns>
        private string ExtractToken(HttpContext context)
        {
            var token = context.Request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token)) return token;

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                return authHeader.Substring("Bearer ".Length).Trim();

            return string.Empty;
        }

        /// <summary> 
        /// 기존 연결 정리 후 새 연결 등록 
        /// <summary>
        /// 주어진 사용자 ID로 웹소켓 연결을 등록한다. 동일한 사용자에 대한 기존 연결이 있으면 먼저 비동기으로 끊고 새 연결을 등록한 뒤 서버 측 연결을 생성한다.
        /// </summary>
        /// <param name="userId">등록할 사용자의 GUID 식별자.</param>
        /// <param name="socket">등록할 클라이언트의 열린 WebSocket 인스턴스.</param>
        private async Task RegisterConnection(Guid userId, WebSocket socket)
        {
            if (_connectionRegistry.TryGet(userId.ToString(), out var existing) && existing != null) {
                _logger.LogInformation("기존 연결 정리: {UserId}", userId);
                await _webSocketService.DisconnectAsync(userId.ToString());
            }

            var connection = new WebSocketClientConnection(userId.ToString(), socket);
            _connectionRegistry.Register(userId.ToString(), connection);
            await _webSocketService.ConnectAsync(userId.ToString());
        }

        /// <summary> 
        /// 세션 루프 실행 
        /// <summary>
        /// 인증된 사용자에 대한 WebSocket 세션을 비동기로 유지하고, 클라이언트의 종료 요청을 감지해 세션을 정리합니다.
        /// </summary>
        /// <param name="socket">활성화된 WebSocket 연결.</param>
        /// <param name="userId">JWT에서 추출된 사용자 ID(문자열 형태의 GUID). 세션이 종료되면 이 ID로 연결이 해제됩니다.</param>
        private async Task RunSessionLoop(WebSocket socket, string userId)
        {
            var buffer = new byte[1024];
            try {
                while (socket.State == WebSocketState.Open) {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close) {
                        _logger.LogInformation("연결 종료 요청: {UserId}", userId);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "세션 루프 오류: {UserId}", userId);
            }
            finally {
                _logger.LogInformation("연결 해제: {UserId}", userId);
                await _webSocketService.DisconnectAsync(userId);
            }
        }
    }
}
