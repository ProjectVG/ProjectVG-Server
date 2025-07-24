using ProjectVG.Application.Services.Conversation;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class ConversationHistoryPreprocessor
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationHistoryPreprocessor> _logger;

        public ConversationHistoryPreprocessor(IConversationService conversationService, ILogger<ConversationHistoryPreprocessor> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        public async Task<List<string>> CollectConversationHistoryAsync(Guid userId, Guid chracterId)
        {
            try
            {
                var history = await _conversationService.GetConversationHistoryAsync(userId, chracterId, 10);
                return history.Select(h => $"{h.Role}: {h.Content}").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "대화 기록 수집 실패: 유저 {userId}", userId);
                return new List<string>();
            }
        }
    }
} 