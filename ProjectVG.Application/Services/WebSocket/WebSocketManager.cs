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

        public async Task<string> ConnectAsync(string userId)
        {
            _logger.LogInformation("새 WebSocket 세션 생성: {UserId}", userId);

            await _sessionStorage.CreateAsync(new SessionInfo {
                SessionId = userId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });

            return userId;
        }

        public async Task SendAsync(string userId, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(userId, json);
            _logger.LogDebug("WebSocket 메시지 전송: {UserId}, 타입: {MessageType}", userId, message.Type);
        }

        public async Task SendTextAsync(string userId, string text)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null) {
                await connection.SendTextAsync(text);
                _logger.LogDebug("WebSocket 텍스트 전송: {UserId}", userId);
            }
            else {
                _logger.LogWarning("사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        public async Task SendBinaryAsync(string userId, byte[] data)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null) {
                await connection.SendBinaryAsync(data);
                _logger.LogDebug("WebSocket 바이너리 전송: {UserId}, {Length} bytes", userId, data?.Length ?? 0);
            }
            else {
                _logger.LogWarning("사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        public Task DisconnectAsync(string userId)
        {
            _connectionRegistry.Unregister(userId);
            _logger.LogInformation("WebSocket 세션 해제: {UserId}", userId);
            return Task.CompletedTask;
        }

        public bool IsSessionActive(string userId)
        {
            return _connectionRegistry.IsConnected(userId);
        }

        public Task UpdateSessionHeartbeatAsync(string userId)
        {
            // 로컬 WebSocket 매니저는 별도 하트비트 업데이트가 필요 없음
            _logger.LogDebug("로컬 WebSocket 매니저 하트비트 (no-op): {UserId}", userId);
            return Task.CompletedTask;
        }
    }
}
