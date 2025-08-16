using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Conversation
{
    public interface IConversationRepository
    {
        Task<IEnumerable<ConversationHistory>> GetByUserIdAsync(Guid userId, Guid characterId, int count = 10);
        Task<ConversationHistory> AddAsync(ConversationHistory conversationHistory);
        Task ClearSessionAsync(Guid userId, Guid characterId);
        Task<int> GetMessageCountAsync(Guid userId, Guid characterId);
    }
} 