using ProjectVG.Domain.Common;


namespace ProjectVG.Domain.Entities.ConversationHistorys
{
    /// <summary>
    /// 대화 기록 엔티티
    /// 
    /// 대화 관리:
    /// - 사용자와 AI 캐릭터 간의 대화 기록
    /// - 역할별 메시지 구분 (user, assistant, system)
    /// - 실제 사용자 요청 시간 기록
    /// </summary>
    public class ConversationHistory : BaseEntity
    {
        /// <summary>
        /// 대화 기록 고유 ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// 캐릭터 ID
        /// </summary>
        public Guid CharacterId { get; set; }
        
        /// <summary>
        /// 사용자 ID
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 채팅 역할 (user, assistant, system - 소문자 문자열)
        /// </summary>
        public string Role { get; set; } = string.Empty;
        
        /// <summary>
        /// 대화 내용
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 사용자가 실제 요청한 시각 (서버 시간이 아닌 클라이언트 기준 시간)
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 대화 세션 ID (하나의 대화에서 여러 메시지를 그룹화하기 위한 ID, nullable)
        /// OpenAI의 requestId 등을 저장
        /// </summary>
        public string? ConversationId { get; set; }
    }
} 