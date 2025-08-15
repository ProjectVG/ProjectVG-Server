using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Enums;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Character;

namespace ProjectVG.Application.Services.Conversation
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(IConversationRepository conversationRepository, ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, ChatRole role, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) {
                throw new ValidationException(ErrorCode.MESSAGE_EMPTY, content);
            }

            if (content.Length > 1000) {
                throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, content.Length);
            }

            var message = new ConversationHistory {
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            var addedMessage = await _conversationRepository.AddAsync(message);
            return addedMessage;
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int count = 10)
        {
            if (count <= 0 || count > 100) {
                throw new ValidationException(ErrorCode.VALIDATION_FAILED, count);
            }

            var history = await _conversationRepository.GetByUserIdAsync(userId, characterId, count);
            return history;
        }

        public async Task ClearConversationAsync(Guid userId, Guid characterId)
        {
            await _conversationRepository.ClearSessionAsync(userId, characterId);
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var count = await _conversationRepository.GetMessageCountAsync(userId, characterId);
            return count;
        }
    }
}
