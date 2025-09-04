using ProjectVG.Application.Models.Chat;
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
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage, context.UserRequestAt, context.RequestId.ToString());
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, context.Response, DateTime.UtcNow, context.RequestId.ToString());
            await PersistMemoryAsync(context);

            _logger.LogDebug("채팅 결과 저장 완료: 세션 {UserId}, 사용자 {UserId}", context.RequestId, context.UserId);
        }

        private async Task PersistMemoryAsync(ChatProcessContext context)
        {
            var episodicRequest = new EpisodicInsertRequest
            {
                Text = context.Response,
                UserId = context.UserId.ToString(),
                Speaker = "assistant",
                Emotion = new EmotionInfo
                {
                    Valence = "neutral",
                    Arousal = "medium",
                    Labels = new List<string> { "helpful", "informative" },
                    Intensity = 0.6
                },
                Context = new Dictionary<string, object>
                {
                    { "character_id", context.CharacterId },
                    { "session_id", context.RequestId },
                    { "conversation_turn", DateTime.UtcNow.Ticks },
                    { "user_message", context.UserMessage },
                    { "response_type", "chat_response" },
                    { "processing_time", 0 }
                },
                ImportanceScore = 0.7
            };

            var userMemoryRequest = new EpisodicInsertRequest
            {
                Text = context.UserMessage,
                UserId = context.UserId.ToString(),
                Speaker = "user",
                Emotion = new EmotionInfo
                {
                    Valence = "neutral",
                    Arousal = "medium",
                    Labels = new List<string> { "inquiry", "conversation" },
                    Intensity = 0.5
                },
                Context = new Dictionary<string, object>
                {
                    { "character_id", context.CharacterId },
                    { "session_id", context.RequestId },
                    { "conversation_turn", DateTime.UtcNow.Ticks - 1 },
                    { "message_type", "user_input" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                },
                ImportanceScore = 0.8
            };

            try
            {
                await _memoryClient.InsertEpisodicAsync(userMemoryRequest);
                await _memoryClient.InsertEpisodicAsync(episodicRequest);
                
                _logger.LogDebug("메모리 삽입 성공: 사용자={UserId}, 캐릭터={CharacterId}", context.UserId, context.CharacterId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "메모리 삽입 실패: 사용자={UserId}, 캐릭터={CharacterId}", context.UserId, context.CharacterId);
            }
        }
    }
}
