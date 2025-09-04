using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Domain.Repositories
{
    public interface IConversationRepository
    {
        Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int page = 1, int pageSize = 10);
        Task<ConversationHistory> AddAsync(ConversationHistory conversationHistory);
        Task DeleteConversationAsync(Guid userId, Guid characterId);
        Task<int> GetMessageCountAsync(Guid userId, Guid characterId);
        Task<IEnumerable<ConversationHistory>> GetByConversationIdAsync(string conversationId, Guid userId);
        Task DeleteMessageAsync(Guid messageId);
    }
}