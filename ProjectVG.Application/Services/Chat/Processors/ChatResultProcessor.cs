using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

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
        /// 채팅 처리 컨텍스트의 사용자 메시지와 AI 응답을 대화 기록에 저장하고 메모리에도 영구화합니다.
        /// </summary>
        /// <param name="context">저장할 세션 및 메시지 정보를 포함하는 처리 컨텍스트(사용자 ID, 캐릭터 ID, 사용자 메시지, AI 응답, 세그먼트 등).</param>
        /// <returns>비동기 작업을 나타내는 Task.</returns>
        public async Task PersistResultsAsync(ChatProcessContext context)
        {
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, context.Response);
            await PersistMemoryAsync(context);

            _logger.LogDebug("채팅 결과 저장 완료: 세션 {UserId}, 사용자 {UserId}", context.SessionId, context.UserId);
        }

        /// <summary>
        /// Assistant의 응답(context.Response)을 메모리 저장소에 비동기으로 삽입합니다.
        /// </summary>
        /// <param name="context">저장할 텍스트(응답), 사용자 ID 등을 포함한 처리 컨텍스트. Response가 메모리의 Text로, UserId가 UserId로 사용되며 Speaker는 "ai"로 설정됩니다.</param>
        /// <remarks>
        /// 내부에서 발생한 예외는 캡처되어 경고 로그로 기록되며 호출자에게 전파되지 않습니다.
        /// </remarks>
        private async Task PersistMemoryAsync(ChatProcessContext context)
        {
            var insert = new MemoryInsertRequest
            {
                Text = context.Response,
                UserId = context.UserId.ToString(),
                Speaker = "ai"
            };

            try
            {
                await _memoryClient.InsertAutoAsync(insert);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "메모리 삽입 실패");
            }
        }

        /// <summary>
        /// 컨텍스트의 세그먼트들을 순서대로 클라이언트에 WebSocket으로 전송한다.
        /// 빈 세그먼트는 건너뛰며, 각 전송 메시지는 세션 ID, 텍스트 및 오디오 메타데이터(형식, 길이, 타임스탬프)와 오디오 데이터를 포함한다.
        /// </summary>
        /// <param name="context">전송할 세그먼트(순서, 텍스트, 오디오 데이터/메타데이터)와 대상 식별자(UserId, SessionId)를 포함하는 처리 컨텍스트.</param>
        public async Task SendResultsAsync(ChatProcessContext context)
        {
            foreach (var segment in context.Segments.OrderBy(s => s.Order)) {
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
                await _webSocketService.SendAsync(context.UserId.ToString(), wsMessage);
            }

            _logger.LogDebug("채팅 결과 전송 완료: 세션 {UserId}, 세그먼트 {SegmentCount}개",
                context.SessionId, context.Segments.Count(s => !s.IsEmpty));
        }
    }
}
