using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ChatFailureHandler
    {
        private readonly ILogger<ChatFailureHandler> _logger;
        private readonly IWebSocketManager _webSocketService;
        private readonly IConversationService _conversationService;
        private readonly IMemoryClient _memoryClient;

        public ChatFailureHandler(
            ILogger<ChatFailureHandler> logger,
            IWebSocketManager webSocketService,
            IConversationService conversationService,
            IMemoryClient memoryClient)
        {
            _logger = logger;
            _webSocketService = webSocketService;
            _conversationService = conversationService;
            _memoryClient = memoryClient;
        }

        public Task HandleFailureAsync(ChatPreprocessContext context, Exception exception)
        {
            _logger.LogError(exception, "채팅 처리 실패: 세션 {SessionId}", context.SessionId);
            return SendErrorMessageAsync(context, "요청 처리 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
        }

        private async Task SendErrorMessageAsync(ChatPreprocessContext context, string errorMessage)
        {
            try
            {
                var errorResponse = new WebSocketMessage("error", new { message = errorMessage });
                await _webSocketService.SendAsync(context.SessionId, errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오류 메시지 전송 실패: 세션 {SessionId}", context.SessionId);
            }
        }
    }
}
