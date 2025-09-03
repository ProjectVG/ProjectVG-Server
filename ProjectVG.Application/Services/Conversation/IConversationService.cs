using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Services.Conversation
{
    public interface IConversationService
    {
        /// <summary>
        /// 대화 메시지를 추가합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="role">메시지 역할 (user, assistant, system)</param>
        /// <param name="content">메시지 내용</param>
        /// <param name="timestamp">사용자 요청 시각</param>
        /// <param name="conversationId">대화 세션 ID (선택사항)</param>
        /// <returns>추가된 대화 메시지</returns>
        Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, string role, string content, DateTime timestamp, string? conversationId = null);

        /// <summary>
        /// 대화 기록을 페이지네이션으로 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="page">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <returns>대화 기록 목록</returns>
        Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int page = 1, int pageSize = 10);

        /// <summary>
        /// 대화 기록을 완전 삭제합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        Task DeleteConversationAsync(Guid userId, Guid characterId);

        /// <summary>
        /// 총 메시지 수를 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <returns>메시지 수</returns>
        Task<int> GetMessageCountAsync(Guid userId, Guid characterId);

        /// <summary>
        /// 특정 대화 세션의 메시지들을 조회합니다
        /// </summary>
        /// <param name="conversationId">대화 세션 ID</param>
        /// <returns>대화 기록 목록</returns>
        Task<IEnumerable<ConversationHistory>> GetByConversationIdAsync(string conversationId);

        /// <summary>
        /// 특정 메시지를 삭제합니다
        /// </summary>
        /// <param name="messageId">메시지 ID</param>
        /// <param name="userId">사용자 ID (권한 확인용)</param>
        Task DeleteMessageAsync(Guid messageId, Guid userId);
    }
} 