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

        public WebSocketManager(
            ILogger<IWebSocketManager> logger,
            IConnectionRegistry connectionRegistry,
            ISessionStorage sessionStorage)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _sessionStorage = sessionStorage;
        }

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

        public async Task SendAsync(string sessionId, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(sessionId, json);
            _logger.LogDebug("WebSocket 메시지 전송: {SessionId}, 타입: {MessageType}", sessionId, message.Type);
        }

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

        public Task DisconnectAsync(string sessionId)
        {
            _connectionRegistry.Unregister(sessionId);
            _logger.LogInformation("WebSocket 세션 해제: {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        public bool IsSessionActive(string sessionId)
        {
            return _connectionRegistry.IsConnected(sessionId);
        }

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

        public Task HandleBinaryMessageAsync(string sessionId, byte[] data)
        {
            _logger.LogDebug("WebSocket 바이너리 메시지 처리: {SessionId}, {Length} bytes", sessionId, data?.Length ?? 0);

            // 바이너리 메시지 처리 로직 구현
            // 예: 오디오 데이터, 파일 업로드 등
            return Task.CompletedTask;
        }

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

        private async Task HandlePingAsync(string sessionId)
        {
            var pongMessage = new WebSocketMessage("pong", new { timestamp = DateTime.UtcNow });
            await SendAsync(sessionId, pongMessage);
        }

        private Task HandleChatMessageAsync(string sessionId, WebSocketMessage message)
        {
            // 채팅 메시지 처리 로직
            // ChatService와 연동하여 처리
            _logger.LogInformation("채팅 메시지 수신: {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        private string GenerateSessionId()
        {
            return $"session_{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
        }
    }
}
