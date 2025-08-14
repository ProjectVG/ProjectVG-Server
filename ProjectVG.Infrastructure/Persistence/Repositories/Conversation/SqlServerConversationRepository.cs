using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.ConversationHistorys;
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

        public async Task<IEnumerable<ConversationHistory>> GetByUserIdAsync(Guid userId, Guid characterId, int count = 10)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted)
                .OrderByDescending(ch => ch.Timestamp)
                .Take(count)
                .OrderBy(ch => ch.Timestamp)
                .ToListAsync();

            _logger.LogDebug("유저 {UserId}, 캐릭터 {CharacterId}에서 메시지 {Count}개를 조회했습니다", userId, characterId, messages.Count);
            return messages;
        }

        public async Task<ConversationHistory> AddAsync(ConversationHistory message)
        {
            message.Id = Guid.NewGuid();
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.Timestamp = DateTime.UtcNow;
            message.IsDeleted = false;

            _context.ConversationHistories.Add(message);

            await _context.SaveChangesAsync();

            _logger.LogDebug("유저 {UserId}, 캐릭터 {CharacterId}에 메시지를 추가했습니다. 메시지 ID: {MessageId}", 
                message.UserId, message.CharacterId, message.Id);

            return message;
        }

        public async Task ClearSessionAsync(Guid userId, Guid characterId)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsDeleted = true;
                message.Update();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("대화 세션을 삭제했습니다: {UserId}:{CharacterId}", userId, characterId);
        }

        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            var count = await _context.ConversationHistories
                .CountAsync(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted);

            return count;
        }
    }
} 