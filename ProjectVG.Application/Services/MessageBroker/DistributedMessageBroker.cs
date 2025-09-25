using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.MessageBroker;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Domain.Services.Server;
using ProjectVG.Application.Services.Session;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;

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

            try
            {
                _subscriber = redis.GetSubscriber();
                _serverId = serverRegistration.GetServerId();

                InitializeSubscriptions();

                // 최종 성공만 로깅
                _logger.LogInformation("[분산브로커] 초기화 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.MESSAGE_BROKER_INITIALIZATION_FAILED.GetMessage();
                _logger.LogError(ex, "[분산브로커] {ErrorMessage}: ServerId={ServerId}", errorMessage, _serverId);
                throw new ProjectVGException(ErrorCode.MESSAGE_BROKER_INITIALIZATION_FAILED, errorMessage, ex, 500);
            }
        }

        private void InitializeSubscriptions()
        {
            try
            {
                // 이 서버로 오는 메시지 구독
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{_serverId}";
                _subscriber.Subscribe(serverChannel, OnServerMessageReceived);

                // 브로드캐스트 메시지 구독
                _subscriber.Subscribe(BROADCAST_CHANNEL, OnBroadcastMessageReceived);

                // 사용자별 메시지 패턴 구독 (현재 서버에 연결된 사용자들만)
                // 사용자가 연결될 때 동적으로 구독하도록 변경 예정

                _logger.LogDebug("[분산브로커] 구독 초기화 완료: ServerId={ServerId}, ServerChannel={ServerChannel}, BroadcastChannel={BroadcastChannel}", _serverId, serverChannel, BROADCAST_CHANNEL);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.REDIS_SUBSCRIPTION_FAILED.GetMessage();
                _logger.LogError(ex, "[분살브로커] {ErrorMessage}: ServerId={ServerId}", errorMessage, _serverId);
                throw new ProjectVGException(ErrorCode.REDIS_SUBSCRIPTION_FAILED, errorMessage, ex, 500);
            }
        }

        public async Task SendToUserAsync(string userId, object message)
        {
            try
            {
                // 1. 먼저 로컬에 해당 사용자가 있는지 확인
                var isLocalActive = _connectionManager.HasLocalConnection(userId);

                if (isLocalActive)
                {
                    // 로컬에 있으면 직접 전송
                    await SendLocalMessage(userId, message);
                    return;
                }

                // 2. 사용자가 어느 서버에 있는지 확인
                var targetServerId = await _serverRegistration.GetUserServerAsync(userId);

                if (string.IsNullOrEmpty(targetServerId))
                {
                    _logger.LogWarning("[분산브로커] 사용자가 연결된 서버를 찾을 수 없음: UserId={UserId}", userId);
                    return;
                }

                // 3. 해당 서버로 메시지 전송 (서버별 채널 사용)
                var brokerMessage = BrokerMessage.CreateUserMessage(userId, message, _serverId);
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{targetServerId}";

                await _subscriber.PublishAsync(serverChannel, brokerMessage.ToJson());

                _logger.LogDebug("[분산브로커] 분산 메시지 전송 완료: UserId={UserId}, TargetServerId={TargetServerId}", userId, targetServerId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED.GetMessage();
                _logger.LogError(ex, "[분산브로커] {ErrorMessage}: UserId={UserId}", errorMessage, userId);
                throw new ExternalServiceException("분산 메시지 브로커", "SendToUserAsync", ex.Message, ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED);
            }
        }

        public async Task BroadcastAsync(object message)
        {
            try
            {
                var brokerMessage = BrokerMessage.CreateBroadcastMessage(message, _serverId);
                await _subscriber.PublishAsync(BROADCAST_CHANNEL, brokerMessage.ToJson());
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED.GetMessage();
                _logger.LogError(ex, "분산 브로드캐스트 {ErrorMessage}", errorMessage);
                throw new ExternalServiceException("분산 메시지 브로커", "BroadcastAsync", ex.Message, ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED);
            }
        }

        public async Task SendToServerAsync(string serverId, object message)
        {
            try
            {
                var brokerMessage = BrokerMessage.CreateServerMessage(serverId, message, _serverId);
                var serverChannel = $"{SERVER_CHANNEL_PREFIX}:{serverId}";

                await _subscriber.PublishAsync(serverChannel, brokerMessage.ToJson());
                _logger.LogDebug("분산 서버 메시지 전송 완료: TargetServerId={ServerId}, SourceServerId={SourceServerId}", serverId, _serverId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED.GetMessage();
                _logger.LogError(ex, "분산 서버 {ErrorMessage}: TargetServerId={ServerId}, SourceServerId={SourceServerId}", errorMessage, serverId, _serverId);
                throw new ExternalServiceException("분산 메시지 브로커", "SendToServerAsync", ex.Message, ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED);
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

                _logger.LogDebug("사용자 채널 구독 완료: {UserId}", userId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.REDIS_SUBSCRIPTION_FAILED.GetMessage();
                _logger.LogError(ex, "사용자 채널 {ErrorMessage}: UserId={UserId}", errorMessage, userId);
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

                _logger.LogDebug("사용자 채널 구독 해제 완뢬: {UserId}", userId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.REDIS_SUBSCRIPTION_FAILED.GetMessage();
                _logger.LogError(ex, "사용자 채널 구독 해제 {ErrorMessage}: UserId={UserId}", errorMessage, userId);
            }
        }

        private async void OnUserMessageReceived(RedisChannel channel, RedisValue message)
        {
            try
            {
                var brokerMessage = BrokerMessage.FromJson(message!);
                if (brokerMessage?.TargetUserId == null)
                {
                    var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_INVALID_FORMAT.GetMessage();
                    _logger.LogWarning("[분산브로커] {ErrorMessage}: Channel={Channel}, Message={Message}", errorMessage, channel, message);
                    return;
                }

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
                    if (!success)
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
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_PARSING_FAILED.GetMessage();
                _logger.LogError(ex, "[분산브로커] 사용자 메시지 {ErrorMessage}: Channel={Channel}", errorMessage, channel);
            }
        }

        private async void OnServerMessageReceived(RedisChannel channel, RedisValue message)
        {
            try
            {
                var brokerMessage = BrokerMessage.FromJson(message!);
                if (brokerMessage == null)
                {
                    var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_INVALID_FORMAT.GetMessage();
                    _logger.LogWarning("서버 메시지 {ErrorMessage}: Message={Message}", errorMessage, message);
                    return;
                }

                // 사용자 메시지 처리
                if (brokerMessage.MessageType == "user_message" && !string.IsNullOrEmpty(brokerMessage.TargetUserId))
                {
                    // 해당 사용자가 이 서버에 연결되어 있는지 확인
                    if (_connectionManager.HasLocalConnection(brokerMessage.TargetUserId))
                    {
                        // 원본 JSON 문자열을 직접 사용하여 메시지 전달
                        await SendLocalMessageAsJson(brokerMessage.TargetUserId, brokerMessage.Payload);

                        _logger.LogDebug("[분산브로커] 서버간 메시지 라우팅 완료: TargetUserId={TargetUserId}, SourceServerId={SourceServerId}",
                            brokerMessage.TargetUserId, brokerMessage.SourceServerId);
                    }
                    else
                    {
                        var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_USER_NOT_CONNECTED.GetMessage();
                        _logger.LogWarning("[분산브로커] {ErrorMessage}: TargetUserId={TargetUserId}, ServerId={ServerId}",
                            errorMessage, brokerMessage.TargetUserId, _serverId);
                    }
                }
                else if (brokerMessage.MessageType == "server_message")
                {
                    // 다른 서버별 메시지 타입 처리 (향후 확장)
                    }
                else
                {
                    var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_INVALID_FORMAT.GetMessage();
                    _logger.LogWarning("[분산브로커] 알 수 없는 메시지 타입 {ErrorMessage}: MessageType={MessageType}", errorMessage, brokerMessage.MessageType);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_PARSING_FAILED.GetMessage();
                _logger.LogError(ex, "서버 메시지 {ErrorMessage}: Channel={Channel}", errorMessage, channel);
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

                // 현재 서버에 연결된 모든 사용자에게 브로드캐스트
                var activeSessionIds = _connectionManager.GetLocalConnectedSessionIds().ToList();
                if (activeSessionIds.Count == 0)
                {
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
                            _logger.LogError(ex, "브로드캐스트 전송 예외: UserId={UserId}", userId);
                        }
                    });
                    broadcastTasks.Add(task);
                }

                // 모든 전송 완료 대기 (타임아웃 5초)
                await Task.WhenAll(broadcastTasks).ConfigureAwait(false);

                // 최종 성공/실패 대학 요약만 로깅
                if (failureCount > 0)
                {
                    _logger.LogWarning("브로드캐스트 부분 실패: 대상={TotalCount}, 성공={SuccessCount}, 실패={FailureCount}",
                        activeSessionIds.Count, successCount, failureCount);
                }
                else
                {
                    _logger.LogInformation("브로드캐스트 완료: 대상={TotalCount}건 전체 성공", activeSessionIds.Count);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_PARSING_FAILED.GetMessage();
                _logger.LogError(ex, "브로드캐스트 메시지 {ErrorMessage}", errorMessage);
            }
        }

        private async Task SendLocalMessageAsJson(string userId, string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                _logger.LogWarning("[분산브로커] 빈 Payload 수신: UserId={UserId}", userId);
                return;
            }

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
                }
                else
                {
                    // 일반 객체이므로 WebSocketMessage로 래핑 (예상되지 않는 케이스)
                    _logger.LogWarning("[분산브로커] 예상하지 못한 JSON 구조, WebSocketMessage로 래핑: UserId={UserId}", userId);
                    var wrappedMessage = new WebSocketMessage("message", root);
                    messageText = System.Text.Json.JsonSerializer.Serialize(wrappedMessage);
                }

                var success = await _connectionManager.SendTextAsync(userId, messageText);
                if (!success)
                {
                    _logger.LogWarning("[분산브로커] JSON 메시지 전송 실패: UserId={UserId}", userId);
                }
            }
            catch (JsonException ex)
            {
                var errorMessage = ErrorCode.INVALID_JSON_FORMAT.GetMessage();
                _logger.LogError(ex, "[분산브로커] JSON 파싱 {ErrorMessage}: UserId={UserId}, Payload={Payload}", errorMessage, userId, payloadJson);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED.GetMessage();
                _logger.LogError(ex, "[분산브로커] SendLocalMessageAsJson {ErrorMessage}: UserId={UserId}", errorMessage, userId);
                throw new ExternalServiceException("분산 메시지 브로커", "SendLocalMessageAsJson", ex.Message, ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED);
            }
        }

        private async Task SendLocalMessage(string userId, object? message)
        {
            if (message == null)
            {
                _logger.LogWarning("[분산브로커] null 메시지 수신: UserId={UserId}", userId);
                return;
            }

            try
            {
                string messageText;

                // WebSocketMessage는 이미 올바른 형태이므로 그대로 직렬화
                if (message is WebSocketMessage wsMessage)
                {
                    messageText = System.Text.Json.JsonSerializer.Serialize(wsMessage);
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
                if (!success)
                {
                    _logger.LogWarning("[분산브로커] 로컬 연결을 찾을 수 없음: UserId={UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED.GetMessage();
                _logger.LogError(ex, "[분산브로커] SendLocalMessage {ErrorMessage}: UserId={UserId}", errorMessage, userId);
                throw new ExternalServiceException("분산 메시지 브로커", "SendLocalMessage", ex.Message, ErrorCode.DISTRIBUTED_MESSAGE_SEND_FAILED);
            }
        }

        public void Dispose()
        {
            try
            {
                _subscriber?.Unsubscribe($"{SERVER_CHANNEL_PREFIX}:{_serverId}");
                _subscriber?.Unsubscribe(BROADCAST_CHANNEL);
                _logger.LogInformation("[분산브로커] 종료 완료: ServerId={ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                var errorMessage = ErrorCode.REDIS_CONNECTION_ERROR.GetMessage();
                _logger.LogError(ex, "분산 브로커 종료 중 {ErrorMessage}: ServerId={ServerId}", errorMessage, _serverId);
            }
        }
    }
}