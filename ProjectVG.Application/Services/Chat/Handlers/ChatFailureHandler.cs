using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Infrastructure.Integrations.MemoryClient;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatFailureHandler
    {
        private readonly ILogger<ChatFailureHandler> _logger;
        private readonly IWebSocketManager _webSocketService;

        public ChatFailureHandler(
            ILogger<ChatFailureHandler> logger,
            IWebSocketManager webSocketService)
        {
            _logger = logger;
            _webSocketService = webSocketService;
        }

        public async Task HandleAsync(ChatProcessContext context)
        {
            try {
                var errorResponse = new WebSocketMessage("fail", "");
                await _webSocketService.SendAsync(context.UserId.ToString(), errorResponse);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "오류 메시지 전송 실패: 세션 {UserId}", context.RequestId);
            }
        }
    }
}
