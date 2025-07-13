using ProjectVG.Domain.Entities.ConversationHistory;

namespace ProjectVG.Infrastructure.Repositories
{
    public interface IConversationRepository
    {
        Task<IEnumerable<ConversationHistory>> GetBySessionIdAsync(string sessionId, int count = 10);
        Task<ConversationHistory> AddAsync(ConversationHistory message);
        Task ClearSessionAsync(string sessionId);
        Task<int> GetMessageCountAsync(string sessionId);
    }
} 