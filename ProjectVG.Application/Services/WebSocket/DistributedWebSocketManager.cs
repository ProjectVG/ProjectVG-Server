using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Domain.Services.MessageBus;
using ProjectVG.Domain.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.WebSocket
{
    /// <summary>
    /// 분산 WebSocket 관리자 구현
    /// </summary>
    public class DistributedWebSocketManager : IDistributedWebSocketManager
    {
        private readonly IConnectionRegistry _localConnectionRegistry;
        private readonly IDistributedSessionManager _sessionManager;
        private readonly IDistributedMessageBus _messageBus;
        private readonly ISessionStorage _sessionStorage;
        private readonly ILogger<DistributedWebSocketManager> _logger;
        private readonly string _serverId;

        public DistributedWebSocketManager(
            IConnectionRegistry localConnectionRegistry,
            IDistributedSessionManager sessionManager,
            IDistributedMessageBus messageBus,
            ISessionStorage sessionStorage,
            ILogger<DistributedWebSocketManager> logger)
        {
            _localConnectionRegistry = localConnectionRegistry;
            _sessionManager = sessionManager;
            _messageBus = messageBus;
            _sessionStorage = sessionStorage;
            _logger = logger;

            // 서버 ID 생성 (환경변수나 설정에서 가져올 수도 있음)
            _serverId = Environment.MachineName + "-" + Environment.ProcessId;
        }

        public async Task<string> ConnectAsync(string userId, string? serverId = null)
        {
            try
            {
                var currentServerId = serverId ?? _serverId;

                _logger.LogInformation("분산 WebSocket 세션 생성: UserId={UserId}, ServerId={ServerId}",
                    userId, currentServerId);

                // 1. 세션 정보 생성
                var sessionInfo = new SessionInfo
                {
                    SessionId = userId,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow
                };

                // 2. 로컬 세션 스토리지에 저장
                await _sessionStorage.CreateAsync(sessionInfo);

                // 3. 분산 세션 관리자에 등록
                await _sessionManager.RegisterSessionAsync(userId, currentServerId, sessionInfo);

                // 4. 세션 업데이트 메시지 브로드캐스트
                var sessionUpdateMessage = new SessionUpdateMessage
                {
                    UserId = userId,
                    ServerId = currentServerId,
                    Action = "Connect",
                    SessionData = new Dictionary<string, object>
                    {
                        ["connectedAt"] = sessionInfo.ConnectedAt,
                        ["sessionId"] = sessionInfo.SessionId
                    }
                };

                await _messageBus.PublishAsync("channel:session", sessionUpdateMessage);

                _logger.LogInformation("분산 WebSocket 세션 등록 완료: UserId={UserId}, ServerId={ServerId}",
                    userId, currentServerId);

                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 WebSocket 세션 생성 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task DisconnectAsync(string userId)
        {
            try
            {
                _logger.LogInformation("분산 WebSocket 세션 해제: UserId={UserId}", userId);

                // 1. 현재 서버 정보 조회
                var serverId = await _sessionManager.GetUserServerAsync(userId);

                // 2. 로컬 연결 해제
                _localConnectionRegistry.Unregister(userId);

                // 3. 분산 세션 해제
                await _sessionManager.UnregisterSessionAsync(userId);

                // 4. 세션 업데이트 메시지 브로드캐스트
                var sessionUpdateMessage = new SessionUpdateMessage
                {
                    UserId = userId,
                    ServerId = serverId ?? _serverId,
                    Action = "Disconnect",
                    SessionData = new Dictionary<string, object>
                    {
                        ["disconnectedAt"] = DateTime.UtcNow
                    }
                };

                await _messageBus.PublishAsync("channel:session", sessionUpdateMessage);

                _logger.LogInformation("분산 WebSocket 세션 해제 완료: UserId={UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 WebSocket 세션 해제 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task SendAsync(string userId, Models.WebSocket.WebSocketMessage message)
        {
            try
            {
                // 1. 로컬 연결 확인
                if (IsLocalSession(userId))
                {
                    // 로컬 연결이 있으면 직접 전송
                    await SendLocalAsync(userId, message);
                    return;
                }

                // 2. 분산 환경에서 사용자가 연결된 서버 조회
                var targetServerId = await _sessionManager.GetUserServerAsync(userId);
                if (string.IsNullOrEmpty(targetServerId))
                {
                    _logger.LogWarning("사용자 세션을 찾을 수 없음: UserId={UserId}", userId);
                    return;
                }

                // 3. 원격 서버로 메시지 라우팅
                await SendRemoteAsync(userId, targetServerId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket 메시지 전송 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task SendTextAsync(string userId, string text)
        {
            var message = new Models.WebSocket.WebSocketMessage
            {
                Type = "text",
                Data = text
            };

            await SendAsync(userId, message);
        }

        public async Task SendBinaryAsync(string userId, byte[] data)
        {
            var message = new Models.WebSocket.WebSocketMessage
            {
                Type = "binary",
                Data = Convert.ToBase64String(data)
            };

            await SendAsync(userId, message);
        }

        public async Task SendToMultipleAsync(IEnumerable<string> userIds, Models.WebSocket.WebSocketMessage message)
        {
            var tasks = userIds.Select(userId => SendAsync(userId, message));
            await Task.WhenAll(tasks);
        }

        private async Task SendLocalAsync(string userId, Models.WebSocket.WebSocketMessage message)
        {
            if (_localConnectionRegistry.TryGet(userId, out var connection) && connection != null)
            {
                var json = JsonSerializer.Serialize(message);
                await connection.SendTextAsync(json);

                _logger.LogDebug("로컬 WebSocket 메시지 전송: UserId={UserId}, Type={Type}",
                    userId, message.Type);
            }
            else
            {
                _logger.LogWarning("로컬 연결을 찾을 수 없음: UserId={UserId}", userId);
            }
        }

        private async Task SendRemoteAsync(string userId, string targetServerId, Models.WebSocket.WebSocketMessage message)
        {
            var distributedMessage = new Domain.Models.MessageBus.WebSocketMessage
            {
                TargetUserId = userId,
                Payload = message,
                Format = WebSocketMessageFormat.Json
            };

            await _messageBus.SendToUserAsync(userId, distributedMessage);

            _logger.LogDebug("원격 WebSocket 메시지 전송: UserId={UserId}, TargetServer={TargetServer}, Type={Type}",
                userId, targetServerId, message.Type);
        }

        public bool IsLocalSession(string userId)
        {
            return _localConnectionRegistry.IsConnected(userId);
        }

        public async Task<bool> IsSessionActiveAsync(string userId)
        {
            return await _sessionManager.IsSessionActiveAsync(userId);
        }

        public int GetLocalConnectionCount()
        {
            return _localConnectionRegistry.GetActiveConnectionCount();
        }

        public async Task<int> GetGlobalSessionCountAsync()
        {
            return await _sessionManager.GetActiveSessionCountAsync();
        }
    }
}