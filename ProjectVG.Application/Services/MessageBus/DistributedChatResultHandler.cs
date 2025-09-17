using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Models.MessageBus;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.WebSocket;
using System.Text.Json;

namespace ProjectVG.Application.Services.MessageBus
{
    /// <summary>
    /// 분산 메시지 버스에서 수신한 메시지를 처리하는 핸들러
    /// </summary>
    public class DistributedChatResultHandler
    {
        private readonly IConnectionRegistry _localConnectionRegistry;
        private readonly ILogger<DistributedChatResultHandler> _logger;

        public DistributedChatResultHandler(
            IConnectionRegistry localConnectionRegistry,
            ILogger<DistributedChatResultHandler> logger)
        {
            _localConnectionRegistry = localConnectionRegistry;
            _logger = logger;
        }

        /// <summary>
        /// WebSocket 메시지를 처리합니다
        /// </summary>
        public async Task HandleWebSocketMessageAsync(Domain.Models.MessageBus.WebSocketMessage distributedMessage)
        {
            try
            {
                var targetUserId = distributedMessage.TargetUserId;

                // 현재 서버에 해당 사용자가 연결되어 있는지 확인
                if (!_localConnectionRegistry.TryGet(targetUserId, out var connection) || connection == null)
                {
                    _logger.LogDebug("로컬 연결을 찾을 수 없음: UserId={UserId}", targetUserId);
                    return;
                }

                // 메시지 형식에 따라 전송
                switch (distributedMessage.Format)
                {
                    case WebSocketMessageFormat.Json:
                        var jsonPayload = JsonSerializer.Serialize(distributedMessage.Payload);
                        await connection.SendTextAsync(jsonPayload);
                        break;

                    case WebSocketMessageFormat.Text:
                        var textPayload = distributedMessage.Payload?.ToString() ?? string.Empty;
                        await connection.SendTextAsync(textPayload);
                        break;

                    case WebSocketMessageFormat.Binary:
                        if (distributedMessage.Payload is byte[] binaryPayload)
                        {
                            await connection.SendBinaryAsync(binaryPayload);
                        }
                        else if (distributedMessage.Payload is string base64Payload)
                        {
                            var binaryData = Convert.FromBase64String(base64Payload);
                            await connection.SendBinaryAsync(binaryData);
                        }
                        break;

                    default:
                        _logger.LogWarning("지원되지 않는 메시지 형식: {Format}", distributedMessage.Format);
                        return;
                }

                _logger.LogDebug("분산 WebSocket 메시지 전달 완료: UserId={UserId}, Format={Format}",
                    targetUserId, distributedMessage.Format);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 WebSocket 메시지 처리 실패: UserId={UserId}",
                    distributedMessage.TargetUserId);
            }
        }

        /// <summary>
        /// 채팅 결과 메시지를 처리합니다
        /// </summary>
        public async Task HandleChatResultAsync(ChatResultMessage chatResult)
        {
            try
            {
                var targetUserId = chatResult.TargetUserId;

                // 현재 서버에 해당 사용자가 연결되어 있는지 확인
                if (!_localConnectionRegistry.TryGet(targetUserId, out var connection) || connection == null)
                {
                    _logger.LogDebug("채팅 결과 대상 사용자 연결 없음: UserId={UserId}", targetUserId);
                    return;
                }

                // 채팅 결과를 WebSocket 메시지로 변환
                var chatResponseMessage = new Models.WebSocket.WebSocketMessage
                {
                    Type = "chat_result",
                    Data = new
                    {
                        requestId = chatResult.MessageId,
                        conversationId = chatResult.ConversationId,
                        content = chatResult.Content,
                        audioData = chatResult.AudioData != null ? Convert.ToBase64String(chatResult.AudioData) : null,
                        cost = chatResult.Cost,
                        success = chatResult.Success,
                        errorMessage = chatResult.ErrorMessage,
                        timestamp = DateTime.UtcNow
                    }
                };

                var messageJson = JsonSerializer.Serialize(chatResponseMessage);
                await connection.SendTextAsync(messageJson);

                _logger.LogInformation("분산 채팅 결과 전달 완료: UserId={UserId}, MessageId={MessageId}, Success={Success}",
                    targetUserId, chatResult.MessageId, chatResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "분산 채팅 결과 처리 실패: UserId={UserId}, MessageId={MessageId}",
                    chatResult.TargetUserId, chatResult.MessageId);
            }
        }

        /// <summary>
        /// 세션 업데이트 메시지를 처리합니다
        /// </summary>
        public Task HandleSessionUpdateAsync(SessionUpdateMessage sessionUpdate)
        {
            try
            {
                _logger.LogDebug("세션 업데이트 처리: UserId={UserId}, Action={Action}, ServerId={ServerId}",
                    sessionUpdate.UserId, sessionUpdate.Action, sessionUpdate.ServerId);

                // 세션 상태 변경에 따른 로컬 처리
                switch (sessionUpdate.Action.ToLower())
                {
                    case "connect":
                        // 새로운 세션 연결 알림 (필요한 경우 로컬 상태 업데이트)
                        break;

                    case "disconnect":
                        // 세션 해제 알림 (필요한 경우 로컬 정리)
                        if (_localConnectionRegistry.IsConnected(sessionUpdate.UserId))
                        {
                            _localConnectionRegistry.Unregister(sessionUpdate.UserId);
                            _logger.LogInformation("원격 세션 해제로 인한 로컬 정리: UserId={UserId}", sessionUpdate.UserId);
                        }
                        break;

                    case "update":
                        // 세션 정보 업데이트
                        break;

                    default:
                        _logger.LogWarning("알 수 없는 세션 액션: {Action}", sessionUpdate.Action);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 업데이트 처리 실패: UserId={UserId}, Action={Action}",
                    sessionUpdate.UserId, sessionUpdate.Action);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 서버 상태 메시지를 처리합니다
        /// </summary>
        public async Task HandleServerStatusAsync(ServerStatusMessage serverStatus)
        {
            try
            {
                _logger.LogInformation("서버 상태 업데이트: ServerId={ServerId}, Status={Status}",
                    serverStatus.ServerId, serverStatus.Status);

                // 서버 상태 변경에 따른 처리
                switch (serverStatus.Status.ToLower())
                {
                    case "offline":
                        // 오프라인된 서버의 세션들을 다른 서버로 마이그레이션하거나 정리
                        await HandleServerOfflineAsync(serverStatus.ServerId);
                        break;

                    case "online":
                        // 새로운 서버가 온라인 상태가 됨
                        _logger.LogInformation("새 서버 온라인: ServerId={ServerId}", serverStatus.ServerId);
                        break;

                    case "maintenance":
                        // 서버가 유지보수 모드로 전환
                        _logger.LogInformation("서버 유지보수 모드: ServerId={ServerId}", serverStatus.ServerId);
                        break;

                    default:
                        _logger.LogWarning("알 수 없는 서버 상태: {Status}", serverStatus.Status);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 상태 처리 실패: ServerId={ServerId}, Status={Status}",
                    serverStatus.ServerId, serverStatus.Status);
            }
        }

        /// <summary>
        /// 서버 오프라인 상황을 처리합니다
        /// </summary>
        private Task HandleServerOfflineAsync(string offlineServerId)
        {
            try
            {
                // 오프라인된 서버의 세션들에 대한 처리
                // 실제 구현에서는 세션 마이그레이션이나 클라이언트 재연결 유도 등을 수행할 수 있음

                _logger.LogWarning("서버 오프라인 처리: ServerId={ServerId}", offlineServerId);

                // 예: 해당 서버의 사용자들에게 재연결 요청 메시지 전송
                // 또는 다른 서버로 세션 이관 처리
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "서버 오프라인 처리 실패: ServerId={ServerId}", offlineServerId);
            }

            return Task.CompletedTask;
        }
    }
}