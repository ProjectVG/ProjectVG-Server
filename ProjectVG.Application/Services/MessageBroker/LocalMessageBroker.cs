using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Session;

namespace ProjectVG.Application.Services.MessageBroker
{
    /// <summary>
    /// 단일 서버 환경에서 사용하는 로컬 메시지 브로커
    /// 새 아키텍처: WebSocketConnectionManager 사용
    /// </summary>
    public class LocalMessageBroker : IMessageBroker
    {
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ILogger<LocalMessageBroker> _logger;

        public bool IsDistributed => false;

        public LocalMessageBroker(
            IWebSocketConnectionManager connectionManager,
            ILogger<LocalMessageBroker> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task SendToUserAsync(string userId, object message)
        {
            try
            {
                // 새 아키텍처: WebSocketConnectionManager 사용
                string messageText;

                if (message is WebSocketMessage wsMessage)
                {
                    messageText = System.Text.Json.JsonSerializer.Serialize(wsMessage);
                }
                else
                {
                    // 일반 객체인 경우 WebSocket 메시지로 감싸서 전송
                    var wrappedMessage = new WebSocketMessage("message", message);
                    messageText = System.Text.Json.JsonSerializer.Serialize(wrappedMessage);
                }

                var success = await _connectionManager.SendTextAsync(userId, messageText);
                if (success)
                {
                    _logger.LogDebug("[LocalMessageBroker] 로컬 메시지 전송 완료: UserId={UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("[LocalMessageBroker] 로컬 메시지 전송 실패 - 연결을 찾을 수 없음: UserId={UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로컬 메시지 전송 실패: 사용자 {UserId}", userId);
                throw;
            }
        }

        public async Task BroadcastAsync(object message)
        {
            try
            {
                // 로컬 환경에서는 현재 연결된 모든 사용자에게 전송
                // 향후 ConnectionRegistry에서 모든 연결된 사용자 목록을 가져와서 전송하도록 구현 예정
                _logger.LogDebug("로컬 브로드캐스트 메시지 (현재 구현 제한)");

                // TODO: IConnectionRegistry에서 모든 활성 사용자 ID 목록을 가져와서 각각에게 전송
                // var activeUserIds = _connectionRegistry.GetAllActiveUserIds();
                // foreach (var userId in activeUserIds)
                // {
                //     await SendToUserAsync(userId, message);
                // }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로컬 브로드캐스트 전송 실패");
                throw;
            }
        }

        public async Task SendToServerAsync(string serverId, object message)
        {
            // 로컬 환경에서는 서버 간 통신이 필요 없음
            _logger.LogDebug("로컬 환경에서 서버 간 통신 무시: 대상 서버 {ServerId}", serverId);
            await Task.CompletedTask;
        }
    }
}