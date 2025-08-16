using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatResultProcessor
    {
        private readonly ILogger<ChatResultProcessor> _logger;
        private readonly IConversationService _conversationService;
        private readonly IMemoryClient _memoryClient;
        private readonly IWebSocketManager _webSocketService;

        /// <summary>
        /// ChatResultProcessor의 새 인스턴스를 초기화합니다.
        /// </summary>
        public ChatResultProcessor(
            ILogger<ChatResultProcessor> logger,
            IConversationService conversationService,
            IMemoryClient memoryClient,
            IWebSocketManager webSocketService)
        {
            _logger = logger;
            _conversationService = conversationService;
            _memoryClient = memoryClient;
            _webSocketService = webSocketService;
        }

        /// <summary>
        /// 사용자 메시지와 어시스턴트 응답을 대화 저장소에 영속화하고 응답을 메모리 저장소에 추가합니다.
        /// </summary>
        /// <remarks>
        /// - 먼저 사용자의 입력을 대화에 추가한 뒤 어시스턴트의 응답을 추가합니다.
        /// - 어시스턴트의 응답은 지정된 메모리 스토어에도 저장됩니다.
        /// - 모든 작업은 비동기적으로 수행되며 완료 시 디버그 로그를 남깁니다.
        /// </remarks>
        /// <param name="context">현재 채팅 전처리 컨텍스트(세션, 사용자, 캐릭터, 사용자 메시지, 메모리 스토어 등)를 포함합니다.</param>
        /// <param name="result">처리 결과로서 어시스턴트의 응답 및 세그먼트 정보를 포함합니다.</param>
        public async Task PersistResultsAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, result.Response);
            await _memoryClient.AddMemoryAsync(context.MemoryStore, result.Response);

            _logger.LogDebug("채팅 결과 저장 완료: 세션 {SessionId}, 사용자 {UserId}", context.SessionId, context.UserId);
        }

        /// <summary>
        /// 채팅 처리 결과의 세그먼트를 클라이언트로 전송한다.
        /// </summary>
        /// <remarks>
        /// result.Segments를 Order에 따라 순회하여 IsEmpty인 세그먼트는 건너뛴다. 각 비어있지 않은 세그먼트에 대해
        /// IntegratedChatMessage를 생성(세션, 텍스트, 오디오 포맷/길이, UTC 타임스탬프)하고 오디오 데이터를 설정한 뒤,
        /// "chat" 타입의 WebSocketMessage로 래핑하여 해당 세션으로 전송한다.
        /// </remarks>
        /// <param name="context">전송에 필요한 세션 및 사용자 관련 컨텍스트(예: SessionId).</param>
        /// <param name="result">전송할 세그먼트 목록과 처리 결과를 포함하는 객체.</param>
        /// <returns>전송 작업이 완료될 때까지의 비동기 작업을 나타내는 Task.</returns>
        public async Task SendResultsAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            foreach (var segment in result.Segments.OrderBy(s => s.Order)) {
                if (segment.IsEmpty) continue;

                var integratedMessage = new IntegratedChatMessage {
                    SessionId = context.SessionId,
                    Text = segment.Text,
                    AudioFormat = segment.AudioContentType ?? "wav",
                    AudioLength = segment.AudioLength,
                    Timestamp = DateTime.UtcNow
                };

                integratedMessage.SetAudioData(segment.AudioData);

                var wsMessage = new WebSocketMessage("chat", integratedMessage);
                await _webSocketService.SendAsync(context.SessionId, wsMessage);
            }

            _logger.LogDebug("채팅 결과 전송 완료: 세션 {SessionId}, 세그먼트 {SegmentCount}개",
                context.SessionId, result.Segments.Count(s => !s.IsEmpty));
        }
    }
}
