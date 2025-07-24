using ProjectVG.Domain.Entities.ConversationHistory;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Conversation
{
    public interface IConversationService
    {
        /// <summary>
        /// 대화 메시지를 추가합니다
        /// </summary>
        /// <param name="userId">세션 ID</param>
        /// <param name="role">메시지 역할</param>
        /// <param name="content">메시지 내용</param>
        /// <returns>추가된 대화 메시지</returns>
        Task<ConversationHistory> AddMessageAsync(Guid userId, Guid chracterId, ChatRole role, string content);

        /// <summary>
        /// 세션의 대화 기록을 조회합니다
        /// </summary>
        /// <param name="userId">세션 ID</param>
        /// <param name="count">조회할 메시지 수</param>
        /// <returns>대화 기록 목록</returns>
        Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid chracterId, int count = 10);

        /// <summary>
        /// 세션의 대화 기록을 삭제합니다
        /// </summary>
        /// <param name="userId">세션 ID</param>
        Task ClearConversationAsync(Guid userId, Guid chracterId);

        /// <summary>
        /// 세션의 메시지 수를 조회합니다
        /// </summary>
        /// <param name="userId">세션 ID</param>
        /// <returns>메시지 수</returns>
        Task<int> GetMessageCountAsync(Guid userId, Guid chracterId);
    }
} 