using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.MessageBroker;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatFailureHandler
    {
        private readonly ILogger<ChatFailureHandler> _logger;
        private readonly IMessageBroker _messageBroker;

        public ChatFailureHandler(
            ILogger<ChatFailureHandler> logger,
            IMessageBroker messageBroker)
        {
            _logger = logger;
            _messageBroker = messageBroker;
        }

        public async Task HandleAsync(ChatProcessContext context)
        {
            try {
                var errorResponse = new WebSocketMessage("fail", "");
                await _messageBroker.SendToUserAsync(context.UserId.ToString(), errorResponse);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "오류 메시지 전송 실패: 세션 {UserId}", context.RequestId);
            }
        }
    }
}
