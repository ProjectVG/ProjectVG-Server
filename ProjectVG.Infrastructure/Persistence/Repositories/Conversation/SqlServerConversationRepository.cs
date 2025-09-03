using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Repositories;
using ProjectVG.Infrastructure.Persistence.EfCore;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Conversation
{
    public class SqlServerConversationRepository : IConversationRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerConversationRepository> _logger;

        public SqlServerConversationRepository(ProjectVGDbContext context, ILogger<SqlServerConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int page = 1, int pageSize = 10)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId)
                .OrderByDescending(ch => ch.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(ch => ch.Timestamp) // 최종적으로 시간순으로 정렬하여 반환
                .ToListAsync();

            return messages;
        }

        public async Task<ConversationHistory> AddAsync(ConversationHistory message)
        {
            if (message.Id == Guid.Empty)
                message.Id = Guid.NewGuid();
                
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            _context.ConversationHistories.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task DeleteConversationAsync(Guid userId, Guid characterId)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId)
                .ToListAsync();

            if (messages.Any())
            {
                _context.ConversationHistories.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            return await _context.ConversationHistories
                .CountAsync(ch => ch.UserId == userId && ch.CharacterId == characterId);
        }

        public async Task<IEnumerable<ConversationHistory>> GetByConversationIdAsync(string conversationId)
        {
            return await _context.ConversationHistories
                .Where(ch => ch.ConversationId == conversationId)
                .OrderBy(ch => ch.Timestamp)
                .ToListAsync();
        }

        public async Task DeleteMessageAsync(Guid messageId)
        {
            var message = await _context.ConversationHistories
                .FirstOrDefaultAsync(ch => ch.Id == messageId);

            if (message != null)
            {
                _context.ConversationHistories.Remove(message);
                await _context.SaveChangesAsync();
            }
        }
    }
}
