using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.WebSocket
{
    public class WebSocketManager : IWebSocketManager
    {
        private readonly ILogger<IWebSocketManager> _logger;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly ISessionStorage _sessionStorage;

        /// <summary>
        /// WebSocketManager를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 의존성으로 전달된 로거, 연결 레지스트리, 세션 저장소를 내부 필드에 보관하여 이 인스턴스의 WebSocket 세션 관리 및 메시지 송수신에 사용합니다.
        /// </remarks>
        public WebSocketManager(
            ILogger<IWebSocketManager> logger,
            IConnectionRegistry connectionRegistry,
            ISessionStorage sessionStorage)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _sessionStorage = sessionStorage;
        }

        /// <summary>
        /// 새 WebSocket 세션을 생성하거나 지정된 세션 ID를 사용하여 세션을 초기화한 뒤 세션 ID를 반환합니다.
        /// </summary>
        /// <param name="sessionId">선택적 세션 식별자. null 또는 공백이면 내부적으로 새 고유 세션 ID를 생성합니다.</param>
        /// <returns>생성되었거나 사용된 실제 세션 ID 문자열을 비동기적으로 반환합니다.</returns>
        /// <remarks>
        /// 이 메서드는 세션 정보를 영구 저장소에 기록하고(ConnectedAt은 UTC 기준), 클라이언트로 초기 "session" 메시지를 전송합니다.
        /// </remarks>
        public async Task<string> ConnectAsync(string? sessionId = null)
        {
            var actualSessionId = string.IsNullOrWhiteSpace(sessionId) ? GenerateSessionId() : sessionId;

            _logger.LogInformation("새 WebSocket 세션 생성: {SessionId}", actualSessionId);

            await _sessionStorage.CreateAsync(new SessionInfo {
                SessionId = actualSessionId,
                UserId = null,
                ConnectedAt = DateTime.UtcNow
            });

            var sessionData = new WebSocketMessage("session", new { session_id = actualSessionId });
            await SendAsync(actualSessionId, sessionData);

            return actualSessionId;
        }

        /// <summary>
        /// 지정된 세션으로 WebSocket 메시지를 JSON으로 직렬화하여 텍스트 프레임으로 전송합니다.
        /// </summary>
        /// <param name="sessionId">메시지를 전송할 대상 세션의 식별자.</param>
        /// <param name="message">전송할 WebSocket 메시지 객체(이 함수에서 JSON으로 직렬화됨).</param>
        /// <returns>전송 작업이 완료될 때까지 완료되는 비동기 작업(Task).</returns>
        public async Task SendAsync(string sessionId, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(sessionId, json);
            _logger.LogDebug("WebSocket 메시지 전송: {SessionId}, 타입: {MessageType}", sessionId, message.Type);
        }

        /// <summary>
        /// 지정한 세션에 텍스트 프레임을 전송합니다.
        /// </summary>
        /// <remarks>
        /// 지정한 세션의 연결이 등록되어 있으면 해당 연결의 SendTextAsync를 호출해 텍스트를 전송합니다.
        /// 세션을 찾지 못하면 예외를 던지지 않고 작업을 종료합니다(경고 로그만 남김).
        /// </remarks>
        /// <param name="sessionId">텍스트를 전송할 대상 세션의 식별자.</param>
        /// <param name="text">전송할 텍스트 페이로드.</param>
        /// <returns>전송 작업이 완료될 때까지 대기하는 비동기 작업.</returns>
        public async Task SendTextAsync(string sessionId, string text)
        {
            if (_connectionRegistry.TryGet(sessionId, out var connection) && connection != null) {
                await connection.SendTextAsync(text);
                _logger.LogDebug("WebSocket 텍스트 전송: {SessionId}", sessionId);
            }
            else {
                _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// 지정한 세션으로 바이너리 프레임을 전송합니다. 연결이 존재하지 않으면 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="sessionId">바이너리를 받을 대상 세션의 식별자.</param>
        /// <param name="data">전송할 바이트 배열.</param>
        public async Task SendBinaryAsync(string sessionId, byte[] data)
        {
            if (_connectionRegistry.TryGet(sessionId, out var connection) && connection != null) {
                await connection.SendBinaryAsync(data);
                _logger.LogDebug("WebSocket 바이너리 전송: {SessionId}, {Length} bytes", sessionId, data?.Length ?? 0);
            }
            else {
                _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// 지정한 세션 ID에 해당하는 연결을 등록 해제하고 관련 로그를 기록합니다.
        /// </summary>
        /// <param name="sessionId">해제할 세션의 식별자(예: ConnectAsync에서 반환된 sessionId).</param>
        /// <returns>연결 해제 작업이 완료된 후 완료된 <see cref="Task"/>.</returns>
        public Task DisconnectAsync(string sessionId)
        {
            _connectionRegistry.Unregister(sessionId);
            _logger.LogInformation("WebSocket 세션 해제: {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 지정한 세션 ID에 대해 현재 연결이 활성 상태인지 여부를 반환합니다.
        /// </summary>
        /// <param name="sessionId">검사할 세션의 식별자.</param>
        /// <returns>세션에 대한 연결이 존재하고 활성화되어 있으면 true, 그렇지 않으면 false.</returns>
        public bool IsSessionActive(string sessionId)
        {
            return _connectionRegistry.IsConnected(sessionId);
        }

        /// <summary>
        /// 지정된 세션에서 수신한 텍스트 메시지를 WebSocketMessage로 역직렬화하여 처리기로 전달합니다.
        /// </summary>
        /// <param name="sessionId">메시지를 보낸 세션의 식별자.</param>
        /// <param name="message">수신된 텍스트 페이로드(문자열 형식의 JSON WebSocketMessage).</param>
        public async Task HandleMessageAsync(string sessionId, string message)
        {
            try {
                _logger.LogDebug("WebSocket 메시지 처리: {SessionId} - {Message}", sessionId, message);

                var webSocketMessage = JsonSerializer.Deserialize<WebSocketMessage>(message);
                if (webSocketMessage == null) {
                    _logger.LogWarning("잘못된 WebSocket 메시지 형식: {SessionId}", sessionId);
                    return;
                }

                await ProcessMessageAsync(sessionId, webSocketMessage);
            }
            catch (JsonException ex) {
                _logger.LogError(ex, "WebSocket 메시지 JSON 파싱 오류: {SessionId}", sessionId);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "WebSocket 메시지 처리 오류: {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// 들어오는 바이너리 WebSocket 메시지를 처리합니다.
        /// </summary>
        /// <remarks>
        /// 현재는 자리표시자 구현입니다. 오디오 데이터 스트림, 파일 업로드 등 바이너리 페이로드에 대한 실제 처리 로직을 이 메서드에 구현해야 합니다.
        /// </remarks>
        /// <param name="sessionId">메시지를 보낸 세션의 식별자.</param>
        /// <param name="data">수신한 바이너리 페이로드.</param>
        /// <returns>비동기 처리 완료를 나타내는 Task.</returns>
        public Task HandleBinaryMessageAsync(string sessionId, byte[] data)
        {
            _logger.LogDebug("WebSocket 바이너리 메시지 처리: {SessionId}, {Length} bytes", sessionId, data?.Length ?? 0);

            // 바이너리 메시지 처리 로직 구현
            // 예: 오디오 데이터, 파일 업로드 등
            return Task.CompletedTask;
        }

        /// <summary>
        /// 수신된 WebSocket 메시지를 타입에 따라 라우팅하여 처리합니다.
        /// </summary>
        /// <param name="sessionId">메시지를 보낸 세션의 식별자.</param>
        /// <param name="message">처리할 WebSocket 메시지(타입에 따라 적절한 핸들러로 전달).</param>
        /// <returns>처리가 완료될 때까지 완료되는 비동기 작업.</returns>
        private async Task ProcessMessageAsync(string sessionId, WebSocketMessage message)
        {
            switch (message.Type?.ToLower()) {
                case "ping":
                    await HandlePingAsync(sessionId);
                    break;
                case "chat":
                    await HandleChatMessageAsync(sessionId, message);
                    break;
                default:
                    _logger.LogWarning("알 수 없는 메시지 타입: {SessionId}, {MessageType}", sessionId, message.Type);
                    break;
            }
        }

        /// <summary>
        /// 들어온 "ping"에 대해 해당 세션으로 "pong" 응답을 비동기로 전송합니다.
        /// </summary>
        /// <param name="sessionId">응답을 보낼 대상 세션의 식별자.</param>
        /// <returns>응답 전송이 완료될 때까지의 비동기 작업.</returns>
        private async Task HandlePingAsync(string sessionId)
        {
            var pongMessage = new WebSocketMessage("pong", new { timestamp = DateTime.UtcNow });
            await SendAsync(sessionId, pongMessage);
        }

        /// <summary>
        /// 세션으로부터 수신된 채팅 메시지를 처리한다.
        /// </summary>
        /// <remarks>
        /// 현재는 수신 로그만 남기고 비동기 완료 상태를 반환하는 플레이스홀더 구현이다.
        /// 향후 ChatService 등과 연동하여 메시지 브로드캐스트, 저장, 필터링 등의 실제 처리 로직을 구현해야 한다.
        /// </remarks>
        /// <param name="sessionId">메시지를 보낸 세션의 식별자.</param>
        /// <param name="message">수신된 채팅 메시지 객체(타입이 "chat"으로 라우팅된 상태).</param>
        /// <returns>처리가 완료된 비동기 작업(Task).</returns>
        private Task HandleChatMessageAsync(string sessionId, WebSocketMessage message)
        {
            // 채팅 메시지 처리 로직
            // ChatService와 연동하여 처리
            _logger.LogInformation("채팅 메시지 수신: {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 고유한 세션 식별자 문자열을 생성합니다.
        /// </summary>
        /// <returns>형식이 "session_{UtcTicks}_{8자리 GUID 접미사}"인 고유한 세션 ID 문자열.</returns>
        private string GenerateSessionId()
        {
            return $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
        }
    }
}
