using ProjectVG.Domain.Common;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Domain.Entities.ConversationHistorys
{
    public class ConversationHistory : BaseEntity
    {
        // 대화 기록 고유 ID
        public Guid Id { get; set; } = Guid.NewGuid();
        // 캐릭터 ID
        public Guid CharacterId { get; set; }
        // 사용자 ID
        public Guid UserId { get; set; }
        // 채팅 역할 (Users, Assistant, System)
        public ChatRole Role { get; set; }                          
        // 대화 내용
        public string Content { get; set; } = string.Empty;          
        // 대화 발생 시각
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;   
        // 추가 메타데이터 (JSON으로 저장)
        public string MetadataJson { get; set; } = "{}";
        // 삭제 여부
        public bool IsDeleted { get; set; } = false;
    }
} 