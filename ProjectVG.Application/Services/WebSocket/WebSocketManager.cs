using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.WebSocket
{
    /// <summary>
    /// 레거시 WebSocket 관리자 - 기존 코드와의 호환성을 위해 유지
    /// 새로운 분산 기능이 필요한 경우 IDistributedWebSocketManager를 사용하세요
    /// </summary>
    public class WebSocketManager : IWebSocketManager
    {
        private readonly ILogger<IWebSocketManager> _logger;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly ISessionStorage _sessionStorage;
        private readonly IDistributedWebSocketManager? _distributedManager;

        public WebSocketManager(
            ILogger<IWebSocketManager> logger,
            IConnectionRegistry connectionRegistry,
            ISessionStorage sessionStorage,
            IDistributedWebSocketManager? distributedManager = null)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _sessionStorage = sessionStorage;
            _distributedManager = distributedManager;
        }

        public async Task<string> ConnectAsync(string userId)
        {
            _logger.LogInformation("WebSocket 세션 생성: {UserId}", userId);

            // 분산 관리자가 있으면 분산 모드로 동작
            if (_distributedManager != null)
            {
                var serverId = GenerateServerId();
                return await _distributedManager.ConnectAsync(userId, serverId);
            }

            // 레거시 모드: 로컬 세션만 관리
            await _sessionStorage.CreateAsync(new SessionInfo {
                SessionId = userId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });

            return userId;
        }

        public async Task SendAsync(string userId, WebSocketMessage message)
        {
            // 분산 관리자가 있으면 분산 전송
            if (_distributedManager != null)
            {
                await _distributedManager.SendAsync(userId, message);
                return;
            }

            // 레거시 모드: 로컬 전송만
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(userId, json);
            _logger.LogDebug("WebSocket 메시지 전송: {UserId}, 타입: {MessageType}", userId, message.Type);
        }

        public async Task SendTextAsync(string userId, string text)
        {
            // 분산 관리자가 있으면 분산 전송
            if (_distributedManager != null)
            {
                await _distributedManager.SendTextAsync(userId, text);
                return;
            }

            // 레거시 모드: 로컬 전송만
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
            // 분산 관리자가 있으면 분산 전송
            if (_distributedManager != null)
            {
                await _distributedManager.SendBinaryAsync(userId, data);
                return;
            }

            // 레거시 모드: 로컬 전송만
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null) {
                await connection.SendBinaryAsync(data);
                _logger.LogDebug("WebSocket 바이너리 전송: {UserId}, {Length} bytes", userId, data?.Length ?? 0);
            }
            else {
                _logger.LogWarning("사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        public async Task DisconnectAsync(string userId)
        {
            // 분산 관리자가 있으면 분산 해제
            if (_distributedManager != null)
            {
                await _distributedManager.DisconnectAsync(userId);
                return;
            }

            // 레거시 모드: 로컬 해제만
            _connectionRegistry.Unregister(userId);
            _logger.LogInformation("WebSocket 세션 해제: {UserId}", userId);
        }

        public async Task<bool> IsSessionActiveAsync(string userId)
        {
            // 분산 관리자가 있으면 분산 상태 확인
            if (_distributedManager != null)
            {
                return await _distributedManager.IsSessionActiveAsync(userId);
            }

            // 레거시 모드: 로컬 상태만 확인
            return _connectionRegistry.IsConnected(userId);
        }

        /// <summary>
        /// 표준화된 서버 ID 생성
        /// </summary>
        private static string GenerateServerId()
        {
            // 1. 환경변수에서 서버 ID 조회 (최우선)
            var envServerId = Environment.GetEnvironmentVariable("SERVER_ID");
            if (!string.IsNullOrWhiteSpace(envServerId))
            {
                return envServerId.Trim();
            }

            // 2. 표준화된 형식으로 자동 생성
            var machineName = Environment.MachineName.ToLowerInvariant();
            var processId = Environment.ProcessId;
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmm");

            return $"api-server-{machineName}-{processId}-{timestamp}";
        }
    }
}
