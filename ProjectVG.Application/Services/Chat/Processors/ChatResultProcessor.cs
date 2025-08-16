using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Messaging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatResultProcessor
    {
        private readonly ILogger<ChatResultProcessor> _logger;
        private readonly IConversationService _conversationService;
        private readonly IMemoryClient _memoryClient;
        private readonly IMessageBroker _messageBroker;

        public ChatResultProcessor(
            ILogger<ChatResultProcessor> logger,
            IConversationService conversationService,
            IMemoryClient memoryClient,
            IMessageBroker messageBroker)
        {
            _logger = logger;
            _conversationService = conversationService;
            _memoryClient = memoryClient;
            _messageBroker = messageBroker;
        }

        public async Task PersistResultsAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, result.Response);
            await _memoryClient.AddMemoryAsync(context.MemoryStore, result.Response);

            _logger.LogDebug("채팅 결과 저장 완료: 세션 {SessionId}, 사용자 {UserId}", context.SessionId, context.UserId);
        }

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
                await _messageBroker.SendWebSocketMessageAsync(context.SessionId, wsMessage);
            }

            _logger.LogDebug("채팅 결과 전송 완료: 세션 {SessionId}, 세그먼트 {SegmentCount}개",
                context.SessionId, result.Segments.Count(s => !s.IsEmpty));
        }
    }
}
