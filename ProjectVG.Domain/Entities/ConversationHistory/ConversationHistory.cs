using ProjectVG.Domain.Common;


namespace ProjectVG.Domain.Entities.ConversationHistorys
{
    /// <summary>
    /// 대화 기록 엔티티
    /// 
    /// 대화 관리:
    /// - 사용자와 AI 캐릭터 간의 대화 기록
    /// - 역할별 메시지 구분 (User, Assistant, System)
    /// - 메타데이터를 통한 추가 정보 저장
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
        /// 채팅 역할 (User, Assistant, System)
        /// </summary>
        public ChatRole Role { get; set; }
        
        /// <summary>
        /// 대화 내용
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 대화 발생 시각
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 추가 메타데이터 (JSON으로 저장)
        /// </summary>
        public string MetadataJson { get; set; } = "{}";
        
        /// <summary>
        /// 삭제 여부
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
} 