using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Conversation
{
    public interface IConversationService
    {
        /// <summary>
        /// 대화 메시지를 추가합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="role">메시지 역할</param>
        /// <param name="content">메시지 내용</param>
        /// <summary>
/// 지정된 사용자와 캐릭터에 대해 역할과 내용을 가진 대화 메시지를 추가하고 추가된 엔티티를 반환합니다.
/// </summary>
/// <param name="userId">대화를 추가할 사용자 식별자(Guid).</param>
/// <param name="characterId">대화가 속한 캐릭터의 식별자(Guid).</param>
/// <param name="role">메시지의 발신자 역할(예: 시스템, 사용자, 어시스턴트).</param>
/// <param name="content">추가할 메시지의 텍스트 내용.</param>
/// <returns>추가된 ConversationHistory 엔티티.</returns>
        Task<ConversationHistory> AddMessageAsync(Guid userId, Guid characterId, ChatRole role, string content);

        /// <summary>
        /// 세션의 대화 기록을 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="count">조회할 메시지 수</param>
        /// <summary>
/// 지정된 사용자와 캐릭터의 세션 대화 기록을 최대 <paramref name="count"/>개까지 조회합니다.
/// </summary>
/// <param name="userId">조회할 사용자의 ID.</param>
/// <param name="characterId">조회할 캐릭터의 ID.</param>
/// <param name="count">가져올 메시지 개수(기본값: 10).</param>
/// <returns>조회된 ConversationHistory 항목들의 컬렉션(없을 경우 빈 컬렉션).</returns>
        Task<IEnumerable<ConversationHistory>> GetConversationHistoryAsync(Guid userId, Guid characterId, int count = 10);

        /// <summary>
        /// 세션의 대화 기록을 삭제합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <summary>
/// 지정된 사용자와 캐릭터에 대한 세션 대화 기록을 삭제합니다.
/// </summary>
/// <param name="userId">대화를 소유한 사용자(식별자).</param>
/// <param name="characterId">대화가 연결된 캐릭터의 식별자.</param>
        Task ClearConversationAsync(Guid userId, Guid characterId);

        /// <summary>
        /// 세션의 메시지 수를 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="characterId">캐릭터 ID</param>
        /// <summary>
/// 지정한 사용자와 캐릭터 세션에 저장된 메시지의 총 개수를 비동기적으로 반환합니다.
/// </summary>
/// <param name="userId">메시지 개수를 조회할 사용자 ID.</param>
/// <param name="characterId">대상 캐릭터의 ID.</param>
/// <returns>세션에 저장된 메시지의 총 개수.</returns>
        Task<int> GetMessageCountAsync(Guid userId, Guid characterId);
    }
} 