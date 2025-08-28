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
        private readonly IConversationService _conversationService;
        private readonly IMemoryClient _memoryClient;

        /// <summary>
        /// ChatFailureHandler 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 필요한 외부 서비스(로거, 웹소켓 매니저, 대화 및 메모리 서비스)를 주입받아 내부 필드에 할당합니다.
        /// </remarks>
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

        /// <summary>
        /// 채팅 처리 중 발생한 예외를 기록하고 사용자에게 에러 메시지를 WebSocket으로 전송합니다.
        /// </summary>
        /// <param name="context">에러가 발생한 채팅 세션의 컨텍스트(세션/사용자 식별자 포함).</param>
        /// <param name="exception">기록할 예외 객체.</param>
        /// <returns>에러 처리 및 WebSocket 알림 전송 작업을 나타내는 비동기 <see cref="Task"/>.</returns>
        public Task HandleFailureAsync(ChatProcessContext context, Exception exception)
        {
            _logger.LogError(exception, "채팅 처리 실패: 세션 {UserId}", context.SessionId);
            return SendErrorMessageAsync(context, "요청 처리 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
        }

        /// <summary>
        /// 지정된 사용자에게 WebSocket을 통해 에러 메시지 페이로드를 전송합니다.
        /// </summary>
        /// <param name="context">전송 대상의 사용자 식별자(UserId)와 로그용 세션 식별자(SessionId)를 포함하는 처리 컨텍스트.</param>
        /// <param name="errorMessage">사용자에게 전달할 에러 텍스트 메시지.</param>
        private async Task SendErrorMessageAsync(ChatProcessContext context, string errorMessage)
        {
            try
            {
                var errorResponse = new WebSocketMessage("error", new { message = errorMessage });
                await _webSocketService.SendAsync(context.UserId.ToString(), errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오류 메시지 전송 실패: 세션 {UserId}", context.SessionId);
            }
        }
    }
}
