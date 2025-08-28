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

        public async Task PersistResultsAsync(ChatProcessContext context)
        {
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, context.Response);
            await PersistMemoryAsync(context);

            _logger.LogDebug("채팅 결과 저장 완료: 세션 {UserId}, 사용자 {UserId}", context.SessionId, context.UserId);
        }

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
