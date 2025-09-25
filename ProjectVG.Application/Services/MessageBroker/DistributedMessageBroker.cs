using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.MessageBroker;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Domain.Services.Server;
using ProjectVG.Application.Services.Session;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ProjectVG.Application.Services.MessageBroker
{
    /// <summary>
    /// Redis Pub/Sub를 사용하는 분산 메시지 브로커
    /// </summary>
    public class DistributedMessageBroker : IMessageBroker, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly IServerRegistrationService _serverRegistration;
        private readonly ILogger<DistributedMessageBroker> _logger;
        private readonly string _serverId;

        private const string USER_CHANNEL_PREFIX = "user";
        private const string SERVER_CHANNEL_PREFIX = "server";
        private const string BROADCAST_CHANNEL = "broadcast";

        public bool IsDistributed => true;

        public DistributedMessageBroker(
            IConnectionMultiplexer redis,
            IWebSocketConnectionManager connectionManager,
            IServerRegistrationService serverRegistration,
            ILogger<DistributedMessageBroker> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _serverRegistration = serverRegistration ?? throw new ArgumentNullException(nameof(serverRegistration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("[분산브로커] DistributedMessageBroker 생성자 시작");

            try
            {
                _subscriber = redis.GetSubscriber();
                _serverId = serverRegistration.GetServerId();

                _logger.LogInformation("[분산브로커] Redis 연결 상태: IsConnected={IsConnected}, ServerId={ServerId}",
                    redis.IsConnected, _serverId);

                InitializeSubscriptions();

                _logger.LogInformation("[분산브로커] DistributedMessageBroker 생성 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] DistributedMessageBroker 생성자 실패");
                throw;
            }
        }

        private void InitializeSubscriptions()
        {
            try
            {
                _logger.LogInformation("[분산브로커] Redis 구독 초기화 시작: ServerId={ServerId}", _serverId);

                // 이 서버로 오는 메시지 구독
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{_serverId}";
                _logger.LogInformation("[분산브로커] 서버 채널 구독 시작: Channel={Channel}", serverChannel);
                _subscriber.Subscribe(serverChannel, OnServerMessageReceived);
                _logger.LogInformation("[분산브로커] 서버 채널 구독 완료: Channel={Channel}", serverChannel);

                // 브로드캐스트 메시지 구독
                _logger.LogInformation("[분산브로커] 브로드캐스트 채널 구독 시작: Channel={Channel}", BROADCAST_CHANNEL);
                _subscriber.Subscribe(BROADCAST_CHANNEL, OnBroadcastMessageReceived);
                _logger.LogInformation("[분산브로커] 브로드캐스트 채널 구독 완료: Channel={Channel}", BROADCAST_CHANNEL);

                // 사용자별 메시지 패턴 구독 (현재 서버에 연결된 사용자들만)
                // 사용자가 연결될 때 동적으로 구독하도록 변경 예정

                _logger.LogInformation("[분산브로커] 분산 메시지 브로커 구독 초기화 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] 분산 메시지 브로커 구독 초기화 실패: ServerId={ServerId}", _serverId);
                throw;
            }
        }

        public async Task SendToUserAsync(string userId, object message)
        {
            try
            {
                _logger.LogInformation("[분산브로커] SendToUserAsync 시작: UserId={UserId}, ServerId={ServerId}", userId, _serverId);

                // 1. 먼저 로컬에 해당 사용자가 있는지 확인
                var isLocalActive = _connectionManager.HasLocalConnection(userId);
                _logger.LogInformation("[분산브로커] 로컬 세션 확인: UserId={UserId}, IsLocalActive={IsLocalActive}", userId, isLocalActive);

                if (isLocalActive)
                {
                    // 로컬에 있으면 직접 전송
                    await SendLocalMessage(userId, message);
                    _logger.LogInformation("[분산브로커] 로컬 사용자에게 직접 전송 완료: UserId={UserId}", userId);
                    return;
                }

                // 2. 사용자가 어느 서버에 있는지 확인
                var targetServerId = await _serverRegistration.GetUserServerAsync(userId);
                _logger.LogInformation("[분산브로커] 사용자 서버 조회: UserId={UserId}, TargetServerId={TargetServerId}", userId, targetServerId ?? "NULL");

                if (string.IsNullOrEmpty(targetServerId))
                {
                    _logger.LogWarning("[분산브로커] 사용자가 연결된 서버를 찾을 수 없음: UserId={UserId}", userId);
                    return;
                }

                // 3. 해당 서버로 메시지 전송 (서버별 채널 사용)
                var brokerMessage = BrokerMessage.CreateUserMessage(userId, message, _serverId);
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{targetServerId}";

                _logger.LogInformation("[분산브로커] Redis Pub 시작: Channel={Channel}, TargetServerId={TargetServerId}, SourceServerId={SourceServerId}",
                    serverChannel, targetServerId, _serverId);

                await _subscriber.PublishAsync(serverChannel, brokerMessage.ToJson());

                _logger.LogInformation("[분산브로커] Redis Pub 완료: UserId={UserId}, TargetServerId={TargetServerId}", userId, targetServerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] 분산 사용자 메시지 전송 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task BroadcastAsync(object message)
        {
            try
            {
                var brokerMessage = BrokerMessage.CreateBroadcastMessage(message, _serverId);
                await _subscriber.PublishAsync(BROADCAST_CHANNEL, brokerMessage.ToJson());

                _logger.LogDebug("분산 브로드캐스트 메시지 전송: 서버 {ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 브로드캐스트 메시지 전송 실패");
                throw;
            }
        }

        public async Task SendToServerAsync(string serverId, object message)
        {
            try
            {
                var brokerMessage = BrokerMessage.CreateServerMessage(serverId, message, _serverId);
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{serverId}";

                await _subscriber.PublishAsync(serverChannel, brokerMessage.ToJson());
                _logger.LogDebug("분산 서버 메시지 전송: {ServerId}", serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 서버 메시지 전송 실패: {ServerId}", serverId);
                throw;
            }
        }

        /// <summary>
        /// 사용자 연결 시 해당 사용자 채널을 구독합니다
        /// </summary>
        public async Task SubscribeToUserChannelAsync(string userId)
        {
            try
            {
                var userChannel = $"{USER_CHANNEL_PREFIX}:{userId}";
                await _subscriber.SubscribeAsync(userChannel, OnUserMessageReceived);

                // 사용자-서버 매핑 설정
                await _serverRegistration.SetUserServerAsync(userId, _serverId);

                _logger.LogDebug("사용자 채널 구독 시작: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 채널 구독 실패: {UserId}", userId);
            }
        }

        /// <summary>
        /// 사용자 연결 해제 시 해당 사용자 채널 구독을 해제합니다
        /// </summary>
        public async Task UnsubscribeFromUserChannelAsync(string userId)
        {
            try
            {
                var userChannel = $"{USER_CHANNEL_PREFIX}:{userId}";
                await _subscriber.UnsubscribeAsync(userChannel);

                // 사용자-서버 매핑 제거
                await _serverRegistration.RemoveUserServerAsync(userId);

                _logger.LogDebug("사용자 채널 구독 해제: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 채널 구독 해제 실패: {UserId}", userId);
            }
        }

        private async void OnUserMessageReceived(RedisChannel channel, RedisValue message)
        {
            try
            {
                _logger.LogInformation("[분산브로커] Redis Sub 메시지 수신: Channel={Channel}, ServerId={ServerId}", channel, _serverId);

                var brokerMessage = BrokerMessage.FromJson(message!);
                if (brokerMessage?.TargetUserId == null)
                {
                    _logger.LogWarning("[분산브로커] 잘못된 사용자 메시지 형식: {Message}", message);
                    return;
                }

                _logger.LogInformation("[분산브로커] 메시지 파싱 완료: TargetUserId={TargetUserId}, SourceServerId={SourceServerId}, MessageType={MessageType}",
                    brokerMessage.TargetUserId, brokerMessage.SourceServerId, brokerMessage.MessageType);

                // 새 아키텍처: WebSocketConnectionManager 사용
                if (_connectionManager.HasLocalConnection(brokerMessage.TargetUserId))
                {
                    var payloadText = brokerMessage.Payload;
                    if (string.IsNullOrEmpty(payloadText))
                    {
                        _logger.LogWarning("[분산브로커] 빈 Payload 수신: Channel={Channel}, TargetUserId={TargetUserId}", channel, brokerMessage.TargetUserId);
                        return;
                    }

                    var success = await _connectionManager.SendTextAsync(brokerMessage.TargetUserId, payloadText);
                    if (success)
                    {
                        _logger.LogInformation("[분산브로커] 분산 사용자 메시지 처리 완료: TargetUserId={TargetUserId}", brokerMessage.TargetUserId);
                    }
                    else
                    {
                        _logger.LogWarning("[분산브로커] 메시지 전송 실패: TargetUserId={TargetUserId}", brokerMessage.TargetUserId);
                    }
                }
                else
                {
                    _logger.LogWarning("[분산브로커] 대상 사용자가 이 서버에 연결되어 있지 않음: TargetUserId={TargetUserId}, ServerId={ServerId}",
                        brokerMessage.TargetUserId, _serverId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] 사용자 메시지 수신 처리 중 오류: Channel={Channel}", channel);
            }
        }

        private async void OnServerMessageReceived(RedisChannel channel, RedisValue message)
        {
            try
            {
                var brokerMessage = BrokerMessage.FromJson(message!);
                if (brokerMessage == null)
                {
                    _logger.LogWarning("잘못된 서버 메시지 형식: {Message}", message);
                    return;
                }

                // 서버별 메시지 처리 로직
                _logger.LogInformation("[분산브로커] 서버 메시지 수신: MessageType={MessageType}, SourceServerId={SourceServerId}",
                    brokerMessage.MessageType, brokerMessage.SourceServerId);

                // 사용자 메시지 처리
                if (brokerMessage.MessageType == "user_message" && !string.IsNullOrEmpty(brokerMessage.TargetUserId))
                {
                    _logger.LogInformation("[분산브로커] 사용자 메시지 처리 시작: TargetUserId={TargetUserId}",
                        brokerMessage.TargetUserId);

                    // 해당 사용자가 이 서버에 연결되어 있는지 확인
                    if (_connectionManager.HasLocalConnection(brokerMessage.TargetUserId))
                    {
                        _logger.LogInformation("[분산브로커] 원본 Payload JSON: {PayloadJson}", brokerMessage.Payload);

                        // 원본 JSON 문자열을 직접 사용하여 메시지 전달
                        await SendLocalMessageAsJson(brokerMessage.TargetUserId, brokerMessage.Payload);

                        _logger.LogInformation("[분산브로커] 사용자 메시지 전달 완료: TargetUserId={TargetUserId}",
                            brokerMessage.TargetUserId);
                    }
                    else
                    {
                        _logger.LogWarning("[분산브로커] 대상 사용자가 이 서버에 연결되어 있지 않음: TargetUserId={TargetUserId}, ServerId={ServerId}",
                            brokerMessage.TargetUserId, _serverId);
                    }
                }
                else if (brokerMessage.MessageType == "server_message")
                {
                    // 다른 서버별 메시지 타입 처리 (향후 확장)
                    _logger.LogDebug("[분산브로커] 서버 메시지 처리: {MessageType}", brokerMessage.MessageType);
                }
                else
                {
                    _logger.LogWarning("[분산브로커] 알 수 없는 메시지 타입: {MessageType}", brokerMessage.MessageType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 메시지 수신 처리 중 오류: {Channel}", channel);
            }
        }

        private async void OnBroadcastMessageReceived(RedisChannel channel, RedisValue message)
        {
            try
            {
                var brokerMessage = BrokerMessage.FromJson(message!);
                if (brokerMessage == null || brokerMessage.SourceServerId == _serverId)
                {
                    // 자신이 보낸 메시지는 무시
                    return;
                }

                _logger.LogDebug("브로드캐스트 메시지 수신: {MessageType}, SourceServer: {SourceServerId}",
                    brokerMessage.MessageType, brokerMessage.SourceServerId);

                // 현재 서버에 연결된 모든 사용자에게 브로드캐스트
                var activeSessionIds = _connectionManager.GetLocalConnectedSessionIds().ToList();
                if (activeSessionIds.Count == 0)
                {
                    _logger.LogDebug("브로드캐스트 대상 없음: 활성 연결 수 = 0");
                    return;
                }

                var broadcastTasks = new List<Task>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var userId in activeSessionIds)
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var success = await _connectionManager.SendTextAsync(userId, brokerMessage.Payload);
                            if (success)
                            {
                                Interlocked.Increment(ref successCount);
                                _logger.LogTrace("브로드캐스트 전송 성공: UserId={UserId}", userId);
                            }
                            else
                            {
                                Interlocked.Increment(ref failureCount);
                                _logger.LogWarning("브로드캐스트 전송 실패: UserId={UserId}", userId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failureCount);
                            _logger.LogWarning(ex, "브로드캐스트 전송 실패: UserId={UserId}", userId);
                        }
                    });
                    broadcastTasks.Add(task);
                }

                // 모든 전송 완료 대기 (타임아웃 5초)
                await Task.WhenAll(broadcastTasks).ConfigureAwait(false);

                _logger.LogInformation("브로드캐스트 완료: 대상={TotalCount}, 성공={SuccessCount}, 실패={FailureCount}",
                    activeSessionIds.Count, successCount, failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "브로드캐스트 메시지 수신 처리 중 오류");
            }
        }

        private async Task SendLocalMessageAsJson(string userId, string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                _logger.LogWarning("[분산브로커] SendLocalMessageAsJson: Payload가 비어있습니다. UserId={UserId}", userId);
                return;
            }

            _logger.LogInformation("[분산브로커] SendLocalMessageAsJson 시작: UserId={UserId}, PayloadLength={PayloadLength}",
                userId, payloadJson.Length);

            try
            {
                // 원본 JSON이 이미 WebSocketMessage 형태인지 확인
                using var document = JsonDocument.Parse(payloadJson);
                var root = document.RootElement;

                string messageText;

                // WebSocketMessage 구조인지 확인 (type과 data 필드가 있는지)
                if (root.TryGetProperty("type", out var typeProperty) &&
                    root.TryGetProperty("data", out var dataProperty))
                {
                    // 이미 WebSocketMessage 형태이므로 그대로 사용
                    messageText = payloadJson;
                    _logger.LogInformation("[분산브로커] WebSocketMessage 형태 감지: Type={Type}", typeProperty.GetString());
                }
                else
                {
                    // 일반 객체이므로 WebSocketMessage로 래핑 (예상되지 않는 케이스)
                    _logger.LogWarning("[분산브로커] 예상하지 못한 JSON 구조, WebSocketMessage로 래핑: UserId={UserId}", userId);
                    var wrappedMessage = new WebSocketMessage("message", root);
                    messageText = System.Text.Json.JsonSerializer.Serialize(wrappedMessage);
                }

                _logger.LogInformation("[분산브로커] 최종 전송 메시지: {MessageText}", messageText);

                var success = await _connectionManager.SendTextAsync(userId, messageText);
                if (success)
                {
                    _logger.LogInformation("[분산브로커] JSON 메시지 전송 완료: UserId={UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("[분산브로커] JSON 메시지 전송 실패: UserId={UserId}", userId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[분산브로커] JSON 파싱 실패: UserId={UserId}, Payload={Payload}", userId, payloadJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] SendLocalMessageAsJson 실패: UserId={UserId}", userId);
                throw;
            }
        }

        private async Task SendLocalMessage(string userId, object? message)
        {
            if (message == null)
            {
                _logger.LogWarning("[분산브로커] SendLocalMessage: 메시지가 null입니다. UserId={UserId}", userId);
                return;
            }

            _logger.LogInformation("[분산브로커] SendLocalMessage 시작: UserId={UserId}, MessageType={MessageType}",
                userId, message.GetType().Name);

            try
            {
                string messageText;

                // WebSocketMessage는 이미 올바른 형태이므로 그대로 직렬화
                if (message is WebSocketMessage wsMessage)
                {
                    messageText = System.Text.Json.JsonSerializer.Serialize(wsMessage);
                    _logger.LogInformation("[분산브로커] WebSocketMessage 직렬화: UserId={UserId}, Type={Type}",
                        userId, wsMessage.Type);
                }
                else
                {
                    // 다른 객체는 WebSocketMessage로 래핑 (하지만 ChatSuccessHandler에서는 이미 래핑됨)
                    _logger.LogWarning("[분산브로커] 예상하지 못한 객체 타입: {MessageType}, UserId={UserId}",
                        message.GetType().Name, userId);
                    var wrappedMessage = new WebSocketMessage("message", message);
                    messageText = System.Text.Json.JsonSerializer.Serialize(wrappedMessage);
                }

                var success = await _connectionManager.SendTextAsync(userId, messageText);
                if (success)
                {
                    if (message is WebSocketMessage ws)
                    {
                        _logger.LogInformation("[분산브로커] WebSocketMessage 전송 완료: UserId={UserId}, Type={Type}",
                            userId, ws.Type);
                    }
                    else
                    {
                        _logger.LogInformation("[분산브로커] 래핑된 메시지 전송 완료: UserId={UserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("[분산브로커] 로컬 연결을 찾을 수 없음: UserId={UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[분산브로커] SendLocalMessage 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _subscriber?.Unsubscribe($"{SERVER_CHANNEL_PREFIX}:{_serverId}");
                _subscriber?.Unsubscribe(BROADCAST_CHANNEL);
                _logger.LogInformation("분산 메시지 브로커 구독 해제 완료");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 메시지 브로커 해제 중 오류");
            }
        }
    }
}