using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.MessageBroker;
using ProjectVG.Application.Services.Session;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.WebSocket
{
    /// <summary>
    /// 분산 환경을 지원하는 WebSocket 관리자
    /// </summary>
    public class DistributedWebSocketManager : IWebSocketManager
    {
        private readonly ILogger<DistributedWebSocketManager> _logger;
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly ISessionStorage _sessionStorage;
        private readonly DistributedMessageBroker? _distributedBroker;

        public DistributedWebSocketManager(
            ILogger<DistributedWebSocketManager> logger,
            IConnectionRegistry connectionRegistry,
            ISessionStorage sessionStorage,
            IMessageBroker messageBroker)
        {
            _logger = logger;
            _connectionRegistry = connectionRegistry;
            _sessionStorage = sessionStorage;

            // MessageBroker가 분산 브로커인지 확인
            _distributedBroker = messageBroker as DistributedMessageBroker;
        }

        public async Task<string> ConnectAsync(string userId)
        {
            _logger.LogInformation("[분산WebSocket] 새 분산 WebSocket 세션 생성: UserId={UserId}", userId);

            await _sessionStorage.CreateAsync(new SessionInfo
            {
                SessionId = userId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });

            // 분산 환경인 경우 사용자 채널 구독
            if (_distributedBroker != null)
            {
                _logger.LogInformation("[분산WebSocket] 분산 브로커 채널 구독 시작: UserId={UserId}", userId);
                await _distributedBroker.SubscribeToUserChannelAsync(userId);
                _logger.LogInformation("[분산WebSocket] 분산 사용자 채널 구독 완료: UserId={UserId}", userId);
            }
            else
            {
                _logger.LogWarning("[분산WebSocket] 분산 브로커가 null입니다: UserId={UserId}", userId);
            }

            return userId;
        }

        public async Task SendAsync(string userId, WebSocketMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            await SendTextAsync(userId, json);
            _logger.LogDebug("분산 WebSocket 메시지 전송: {UserId}, 타입: {MessageType}", userId, message.Type);
        }

        public async Task SendTextAsync(string userId, string text)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null)
            {
                await connection.SendTextAsync(text);
                _logger.LogDebug("분산 WebSocket 텍스트 전송: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("분산 환경에서 사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        public async Task SendBinaryAsync(string userId, byte[] data)
        {
            if (_connectionRegistry.TryGet(userId, out var connection) && connection != null)
            {
                await connection.SendBinaryAsync(data);
                _logger.LogDebug("분산 WebSocket 바이너리 전송: {UserId}, {Length} bytes", userId, data?.Length ?? 0);
            }
            else
            {
                _logger.LogWarning("분산 환경에서 사용자를 찾을 수 없음: {UserId}", userId);
            }
        }

        public async Task DisconnectAsync(string userId)
        {
            _logger.LogInformation("[분산WebSocket] 분산 WebSocket 세션 해제 시작: UserId={UserId}", userId);

            _connectionRegistry.Unregister(userId);

            // 분산 환경인 경우 사용자 채널 구독 해제
            if (_distributedBroker != null)
            {
                _logger.LogInformation("[분산WebSocket] 분산 브로커 채널 구독 해제 시작: UserId={UserId}", userId);
                await _distributedBroker.UnsubscribeFromUserChannelAsync(userId);
                _logger.LogInformation("[분산WebSocket] 분산 사용자 채널 구독 해제 완료: UserId={UserId}", userId);
            }

            _logger.LogInformation("[분산WebSocket] 분산 WebSocket 세션 해제 완료: UserId={UserId}", userId);
        }

        public bool IsSessionActive(string userId)
        {
            return _connectionRegistry.IsConnected(userId);
        }
    }
}