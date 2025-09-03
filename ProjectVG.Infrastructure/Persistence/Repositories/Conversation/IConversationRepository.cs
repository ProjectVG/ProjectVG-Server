using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Conversation
{
    public interface IConversationRepository
    {
        /// <summary>
        /// 사용자와 캐릭터 간의 대화 기록을 최신순으로 조회 (페이지네이션 지원)
        /// </summary>
        Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int page = 1, int pageSize = 10);
        
        /// <summary>
        /// 대화 메시지 추가
        /// </summary>
        Task<ConversationHistory> AddAsync(ConversationHistory conversationHistory);
        
        /// <summary>
        /// 특정 사용자와 캐릭터의 대화 기록 완전 삭제
        /// </summary>
        Task DeleteConversationAsync(Guid userId, Guid characterId);
        
        /// <summary>
        /// 특정 사용자와 캐릭터의 총 메시지 수 조회
        /// </summary>
        Task<int> GetMessageCountAsync(Guid userId, Guid characterId);
        
        /// <summary>
        /// 대화 ID로 특정 대화 세션의 메시지들 조회
        /// </summary>
        Task<IEnumerable<ConversationHistory>> GetByConversationIdAsync(string conversationId);
        
        /// <summary>
        /// 특정 대화 메시지 단건 삭제
        /// </summary>
        Task DeleteMessageAsync(Guid messageId);
    }
} 