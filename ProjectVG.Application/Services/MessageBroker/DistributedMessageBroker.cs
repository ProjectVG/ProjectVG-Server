using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.MessageBroker;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Domain.Services.Server;
using ProjectVG.Application.Services.WebSocket;
using StackExchange.Redis;

namespace ProjectVG.Application.Services.MessageBroker
{
    /// <summary>
    /// Redis Pub/Sub를 사용하는 분산 메시지 브로커
    /// </summary>
    public class DistributedMessageBroker : IMessageBroker, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly IWebSocketManager _webSocketManager;
        private readonly IServerRegistrationService _serverRegistration;
        private readonly ILogger<DistributedMessageBroker> _logger;
        private readonly string _serverId;

        private const string USER_CHANNEL_PREFIX = "user";
        private const string SERVER_CHANNEL_PREFIX = "server";
        private const string BROADCAST_CHANNEL = "broadcast";

        public bool IsDistributed => true;

        public DistributedMessageBroker(
            IConnectionMultiplexer redis,
            IWebSocketManager webSocketManager,
            IServerRegistrationService serverRegistration,
            ILogger<DistributedMessageBroker> logger)
        {
            _redis = redis;
            _subscriber = redis.GetSubscriber();
            _webSocketManager = webSocketManager;
            _serverRegistration = serverRegistration;
            _logger = logger;
            _serverId = serverRegistration.GetServerId();

            InitializeSubscriptions();
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

                _logger.LogInformation("분산 메시지 브로커 구독 초기화 완료: 서버 {ServerId}", _serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 메시지 브로커 구독 초기화 실패");
            }
        }

        public async Task SendToUserAsync(string userId, object message)
        {
            try
            {
                _logger.LogInformation("[분산브로커] SendToUserAsync 시작: UserId={UserId}, ServerId={ServerId}", userId, _serverId);

                // 1. 먼저 로컬에 해당 사용자가 있는지 확인
                var isLocalActive = _webSocketManager.IsSessionActive(userId);
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

                // 3. 해당 서버로 메시지 전송
                var brokerMessage = BrokerMessage.CreateUserMessage(userId, message, _serverId);
                var userChannel = $"{USER_CHANNEL_PREFIX}:{userId}";

                _logger.LogInformation("[분산브로커] Redis Pub 시작: Channel={Channel}, TargetServerId={TargetServerId}, SourceServerId={SourceServerId}",
                    userChannel, targetServerId, _serverId);

                await _subscriber.PublishAsync(userChannel, brokerMessage.ToJson());

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

                // 로컬에서 해당 사용자가 연결되어 있는지 확인
                var isLocalActive = _webSocketManager.IsSessionActive(brokerMessage.TargetUserId);
                _logger.LogInformation("[분산브로커] 로컬 세션 확인: TargetUserId={TargetUserId}, IsLocalActive={IsLocalActive}",
                    brokerMessage.TargetUserId, isLocalActive);

                if (isLocalActive)
                {
                    var payload = brokerMessage.DeserializePayload<object>();
                    await SendLocalMessage(brokerMessage.TargetUserId, payload);

                    _logger.LogInformation("[분산브로커] 분산 사용자 메시지 처리 완료: TargetUserId={TargetUserId}", brokerMessage.TargetUserId);
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
                _logger.LogDebug("서버 메시지 수신: {MessageType}", brokerMessage.MessageType);

                // TODO: 서버별 메시지 타입에 따른 처리 로직 구현
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

                // 현재 서버에 연결된 모든 사용자에게 브로드캐스트
                // TODO: IConnectionRegistry에서 모든 활성 사용자 목록을 가져와서 전송
                _logger.LogDebug("브로드캐스트 메시지 수신: {MessageType}", brokerMessage.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "브로드캐스트 메시지 수신 처리 중 오류");
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
                if (message is WebSocketMessage wsMessage)
                {
                    await _webSocketManager.SendAsync(userId, wsMessage);
                    _logger.LogInformation("[분산브로커] WebSocketMessage 전송 완료: UserId={UserId}, Type={Type}",
                        userId, wsMessage.Type);
                }
                else
                {
                    var wrappedMessage = new WebSocketMessage("message", message);
                    await _webSocketManager.SendAsync(userId, wrappedMessage);
                    _logger.LogInformation("[분산브로커] 래핑된 메시지 전송 완료: UserId={UserId}", userId);
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