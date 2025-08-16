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

        /// <summary>
        /// 지정한 사용자와 캐릭터에 대해 삭제되지 않은 최근 대화 기록을 최대 <paramref name="count"/>개 가져옵니다.
        /// </summary>
        /// <param name="userId">검색할 사용자 식별자.</param>
        /// <param name="characterId">검색할 캐릭터 식별자.</param>
        /// <param name="count">가져올 최대 메시지 수(기본값 10).</param>
        /// <returns>가장 최근의 메시지들 중 최대 <paramref name="count"/>개를 시간순(오래된 것부터 최신)으로 정렬한 <see cref="IEnumerable{ConversationHistory}"/>.</returns>
        public async Task<IEnumerable<ConversationHistory>> GetByUserIdAsync(Guid userId, Guid characterId, int count = 10)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted)
                .OrderByDescending(ch => ch.Timestamp)
                .Take(count)
                .OrderBy(ch => ch.Timestamp)
                .ToListAsync();

            return messages;
        }

        /// <summary>
        /// 새 ConversationHistory 엔티티의 기본 필드를 초기화하고 영속화한 뒤 추가된 엔티티를 반환합니다.
        /// </summary>
        /// <param name="message">데이터베이스에 추가할 ConversationHistory 객체(이 메서드에서 Id와 타임스탬프가 설정됩니다).</param>
        /// <returns>DB에 저장된 동일한 ConversationHistory 인스턴스(생성된 Id 및 UTC 타임스탬프 포함).</returns>
        public async Task<ConversationHistory> AddAsync(ConversationHistory message)
        {
            message.Id = Guid.NewGuid();
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.Timestamp = DateTime.UtcNow;
            message.IsDeleted = false;

            _context.ConversationHistories.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        /// <summary>
        /// 지정한 사용자(userId)와 캐릭터(characterId)에 대한 대화 기록을 모두 소프트 삭제(IsDeleted = true)하고 변경 내용을 저장합니다.
        /// </summary>
        /// <param name="userId">삭제 대상 대화 기록이 속한 사용자 식별자.</param>
        /// <param name="characterId">삭제 대상 대화 기록이 속한 캐릭터 식별자.</param>
        public async Task ClearSessionAsync(Guid userId, Guid characterId)
        {
            var messages = await _context.ConversationHistories
                .Where(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted)
                .ToListAsync();

            foreach (var message in messages) {
                message.IsDeleted = true;
                message.Update();
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 지정된 사용자와 캐릭터에 대한 삭제되지 않은 대화 기록 수를 비동기적으로 반환합니다.
        /// </summary>
        /// <param name="userId">대화 기록을 조회할 사용자 식별자.</param>
        /// <param name="characterId">대화 기록을 조회할 캐릭터 식별자.</param>
        /// <returns>해당 사용자와 캐릭터에 속하며 삭제되지 않은 대화 메시지의 총 개수.</returns>
        public async Task<int> GetMessageCountAsync(Guid userId, Guid characterId)
        {
            return await _context.ConversationHistories
                .CountAsync(ch => ch.UserId == userId && ch.CharacterId == characterId && !ch.IsDeleted);
        }
    }
}
