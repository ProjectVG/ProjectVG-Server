using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Domain.Services.MessageBus;
using ProjectVG.Domain.Services.Session;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ProjectVG.Infrastructure.MessageBus
{
    /// <summary>
    /// Redis 기반 분산 메시지 버스 구현
    /// </summary>
    public class RedisDistributedMessageBus : IDistributedMessageBus, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly IDistributedSessionManager _sessionManager;
        private readonly ILogger<RedisDistributedMessageBus> _logger;

        // 메시지 핸들러를 위한 콜백 액션들
        private Func<DistributedMessage, Task>? _messageHandler;

        // 채널 상수
        private const string USER_MESSAGE_CHANNEL_PREFIX = "channel:user:";        // channel:user:{userId}
        private const string SERVER_MESSAGE_CHANNEL_PREFIX = "channel:server:";    // channel:server:{serverId}
        private const string BROADCAST_CHANNEL = "channel:broadcast";              // 전체 브로드캐스트
        private const string SERVER_DISCOVERY_CHANNEL = "channel:discovery";      // 서버 발견
        private const string SESSION_UPDATE_CHANNEL = "channel:session";          // 세션 업데이트

        private readonly ConcurrentDictionary<string, Func<DistributedMessage, Task>> _channelHandlers = new();
        private string _currentServerId = string.Empty;
        private bool _isStarted = false;

        public RedisDistributedMessageBus(
            IConnectionMultiplexer redis,
            IDistributedSessionManager sessionManager,
            ILogger<RedisDistributedMessageBus> logger)
        {
            _redis = redis;
            _subscriber = redis.GetSubscriber();
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task SendToUserAsync(string userId, DistributedMessage message)
        {
            try
            {
                message.FromServer = _currentServerId;
                var channel = USER_MESSAGE_CHANNEL_PREFIX + userId;
                await PublishInternalAsync(channel, message);

                _logger.LogDebug("사용자 메시지 발송: UserId={UserId}, MessageType={MessageType}",
                    userId, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 메시지 발송 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task SendToServerAsync(string serverId, DistributedMessage message)
        {
            try
            {
                message.FromServer = _currentServerId;
                var channel = SERVER_MESSAGE_CHANNEL_PREFIX + serverId;
                await PublishInternalAsync(channel, message);

                _logger.LogDebug("서버 메시지 발송: ServerId={ServerId}, MessageType={MessageType}",
                    serverId, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 메시지 발송 실패: ServerId={ServerId}", serverId);
                throw;
            }
        }

        public async Task BroadcastAsync(DistributedMessage message)
        {
            try
            {
                message.FromServer = _currentServerId;
                await PublishInternalAsync(BROADCAST_CHANNEL, message);

                _logger.LogDebug("브로드캐스트 메시지 발송: MessageType={MessageType}", message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "브로드캐스트 메시지 발송 실패");
                throw;
            }
        }

        public async Task PublishAsync(string channel, DistributedMessage message)
        {
            try
            {
                message.FromServer = _currentServerId;
                await PublishInternalAsync(channel, message);

                _logger.LogDebug("채널 메시지 발송: Channel={Channel}, MessageType={MessageType}",
                    channel, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채널 메시지 발송 실패: Channel={Channel}", channel);
                throw;
            }
        }

        private async Task PublishInternalAsync(string channel, DistributedMessage message)
        {
            var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _subscriber.PublishAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), messageJson);
        }

        public async Task SubscribeAsync(string channel, Func<DistributedMessage, Task> handler)
        {
            try
            {
                _channelHandlers[channel] = handler;

                await _subscriber.SubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal), async (ch, message) =>
                {
                    try
                    {
                        if (_channelHandlers.TryGetValue(channel, out var channelHandler))
                        {
                            var distributedMessage = DeserializeMessage(message.HasValue ? message.ToString() : string.Empty);
                            if (distributedMessage != null)
                            {
                                await channelHandler(distributedMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "메시지 처리 실패: Channel={Channel}", channel);
                    }
                });

                _logger.LogInformation("채널 구독 시작: Channel={Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채널 구독 실패: Channel={Channel}", channel);
                throw;
            }
        }

        public async Task UnsubscribeAsync(string channel)
        {
            try
            {
                await _subscriber.UnsubscribeAsync(new RedisChannel(channel, RedisChannel.PatternMode.Literal));
                _channelHandlers.TryRemove(channel, out _);

                _logger.LogInformation("채널 구독 해제: Channel={Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채널 구독 해제 실패: Channel={Channel}", channel);
                throw;
            }
        }

        public async Task StartAsync(string serverId)
        {
            if (_isStarted)
            {
                _logger.LogWarning("메시지 버스가 이미 시작됨: ServerId={ServerId}", serverId);
                return;
            }

            try
            {
                _currentServerId = serverId;

                // 기본 채널들 구독
                await SubscribeToDefaultChannels();

                _isStarted = true;
                _logger.LogInformation("메시지 버스 시작 완료: ServerId={ServerId}", serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 버스 시작 실패: ServerId={ServerId}", serverId);
                throw;
            }
        }

        private async Task SubscribeToDefaultChannels()
        {
            // 1. 현재 서버 전용 채널
            await SubscribeAsync(SERVER_MESSAGE_CHANNEL_PREFIX + _currentServerId, HandleServerMessage);

            // 2. 브로드캐스트 채널
            await SubscribeAsync(BROADCAST_CHANNEL, HandleBroadcastMessage);

            // 3. 서버 발견 채널
            await SubscribeAsync(SERVER_DISCOVERY_CHANNEL, HandleServerDiscoveryMessage);

            // 4. 세션 업데이트 채널
            await SubscribeAsync(SESSION_UPDATE_CHANNEL, HandleSessionUpdateMessage);

            // 5. 현재 서버의 모든 사용자들에 대한 개별 채널 구독
            var serverUsers = await _sessionManager.GetServerUsersAsync(_currentServerId);
            foreach (var userId in serverUsers)
            {
                await SubscribeAsync(USER_MESSAGE_CHANNEL_PREFIX + userId, HandleUserMessage);
            }
        }

        private Task HandleServerMessage(DistributedMessage message)
        {
            _logger.LogDebug("서버 메시지 수신: MessageType={MessageType}, FromServer={FromServer}",
                message.MessageType, message.FromServer);

            // 서버별 메시지 처리 로직
            // 예: 설정 업데이트, 상태 동기화 등
            return Task.CompletedTask;
        }

        private async Task HandleBroadcastMessage(DistributedMessage message)
        {
            _logger.LogDebug("브로드캐스트 메시지 수신: MessageType={MessageType}, FromServer={FromServer}",
                message.MessageType, message.FromServer);

            try
            {
                // 자신이 보낸 메시지는 무시
                if (message.FromServer == _currentServerId)
                {
                    return;
                }

                // 브로드캐스트 메시지 타입별 처리
                switch (message.MessageType)
                {
                    case "ServerAnnouncement":
                        _logger.LogInformation("서버 공지: {Message}", message.ToString());
                        break;

                    case "SystemMaintenance":
                        _logger.LogWarning("시스템 유지보수 공지: {Message}", message.ToString());
                        break;

                    case "GlobalNotification":
                        // 모든 로컬 연결에게 알림 전달
                        _logger.LogInformation("글로벌 알림: {Message}", message.ToString());
                        break;

                    default:
                        _logger.LogDebug("알 수 없는 브로드캐스트 메시지: {MessageType}", message.MessageType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "브로드캐스트 메시지 처리 실패: MessageType={MessageType}", message.MessageType);
            }
        }

        private Task HandleServerDiscoveryMessage(DistributedMessage message)
        {
            if (message is ServerStatusMessage statusMessage)
            {
                _logger.LogInformation("서버 상태 변경: ServerId={ServerId}, Status={Status}",
                    statusMessage.ServerId, statusMessage.Status);

                // 서버 상태 변경에 따른 처리
                if (statusMessage.Status == "Offline")
                {
                    // 오프라인된 서버의 세션들을 정리하거나 다른 서버로 마이그레이션
                }
            }
            return Task.CompletedTask;
        }

        private async Task HandleSessionUpdateMessage(DistributedMessage message)
        {
            if (message is SessionUpdateMessage sessionMessage)
            {
                _logger.LogDebug("세션 업데이트: UserId={UserId}, Action={Action}",
                    sessionMessage.UserId, sessionMessage.Action);

                // 세션 변경에 따른 채널 구독/해제
                if (sessionMessage.Action == "Connect" && sessionMessage.ServerId == _currentServerId)
                {
                    await SubscribeAsync(USER_MESSAGE_CHANNEL_PREFIX + sessionMessage.UserId, HandleUserMessage);
                }
                else if (sessionMessage.Action == "Disconnect")
                {
                    await UnsubscribeAsync(USER_MESSAGE_CHANNEL_PREFIX + sessionMessage.UserId);
                }
            }
        }

        private async Task HandleUserMessage(DistributedMessage message)
        {
            _logger.LogDebug("사용자 메시지 수신: MessageType={MessageType}", message.MessageType);

            try
            {
                // 외부에서 등록된 메시지 핸들러가 있으면 사용
                if (_messageHandler != null)
                {
                    await _messageHandler(message);
                }
                else
                {
                    _logger.LogWarning("메시지 핸들러가 등록되지 않음: MessageType={MessageType}", message.MessageType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 메시지 처리 실패: MessageType={MessageType}", message.MessageType);
            }
        }

        /// <summary>
        /// 메시지 핸들러를 등록합니다
        /// </summary>
        public void SetMessageHandler(Func<DistributedMessage, Task> handler)
        {
            _messageHandler = handler;
        }

        public async Task StopAsync()
        {
            if (!_isStarted)
                return;

            try
            {
                // 모든 채널 구독 해제
                var channels = _channelHandlers.Keys.ToList();
                foreach (var channel in channels)
                {
                    await UnsubscribeAsync(channel);
                }

                _isStarted = false;
                _logger.LogInformation("메시지 버스 중지 완료: ServerId={ServerId}", _currentServerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 버스 중지 실패: ServerId={ServerId}", _currentServerId);
                throw;
            }
        }

        private DistributedMessage? DeserializeMessage(string messageJson)
        {
            try
            {
                using var document = JsonDocument.Parse(messageJson);
                var messageType = document.RootElement.GetProperty("messageType").GetString();

                return messageType switch
                {
                    "websocket_message" => JsonSerializer.Deserialize<WebSocketMessage>(messageJson, GetJsonOptions()),
                    "server_status" => JsonSerializer.Deserialize<ServerStatusMessage>(messageJson, GetJsonOptions()),
                    "chat_result" => JsonSerializer.Deserialize<ChatResultMessage>(messageJson, GetJsonOptions()),
                    "session_update" => JsonSerializer.Deserialize<SessionUpdateMessage>(messageJson, GetJsonOptions()),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 역직렬화 실패: {Message}", messageJson);
                return null;
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public void Dispose()
        {
            if (_isStarted)
            {
                StopAsync().Wait();
            }
        }
    }
}