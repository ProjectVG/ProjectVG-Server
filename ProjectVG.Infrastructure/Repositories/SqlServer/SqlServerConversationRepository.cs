using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.ConversationHistory;
using ProjectVG.Infrastructure.Data;

namespace ProjectVG.Infrastructure.Repositories.SqlServer
{
    public class SqlServerConversationRepository : IConversationRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerConversationRepository> _logger;
        private const int MAX_CONVERSATION_MESSAGES = 50;

        public SqlServerConversationRepository(ProjectVGDbContext context, ILogger<SqlServerConversationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ConversationHistory>> GetBySessionIdAsync(string sessionId, int count = 10)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.SessionId == sessionId && !ch.IsDeleted)
                .OrderByDescending(ch => ch.Timestamp)
                .Take(count)
                .OrderBy(ch => ch.Timestamp)
                .ToListAsync();

            _logger.LogDebug("세션 {SessionId}에서 메시지 {Count}개를 조회했습니다", sessionId, messages.Count);
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

            // 최대 메시지 수 제한을 위해 오래된 메시지 삭제
            var sessionMessages = await _context.ConversationHistories
                .Where(ch => ch.SessionId == message.SessionId && !ch.IsDeleted)
                .OrderByDescending(ch => ch.Timestamp)
                .Skip(MAX_CONVERSATION_MESSAGES)
                .ToListAsync();

            if (sessionMessages.Any())
            {
                foreach (var oldMessage in sessionMessages)
                {
                    oldMessage.IsDeleted = true;
                    oldMessage.Update();
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("세션 {SessionId}에 메시지를 추가했습니다. 메시지 ID: {MessageId}", 
                message.SessionId, message.Id);

            return message;
        }

        public async Task ClearSessionAsync(string sessionId)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.SessionId == sessionId && !ch.IsDeleted)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsDeleted = true;
                message.Update();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("대화 세션을 삭제했습니다: {SessionId}", sessionId);
        }

        public async Task<int> GetMessageCountAsync(string sessionId)
        {
            var count = await _context.ConversationHistories
                .CountAsync(ch => ch.SessionId == sessionId && !ch.IsDeleted);

            return count;
        }
    }
} 