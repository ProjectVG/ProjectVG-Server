using ProjectVG.Domain.Common;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Domain.Entities.ConversationHistory
{
    public class ConversationHistory : BaseEntity
    {
        // 대화 기록 고유 ID
        public string Id { get; set; } = Guid.NewGuid().ToString(); 
        // 대화 세션 ID
        public string SessionId { get; set; } = string.Empty;        
        // 채팅 역할 (User, Assistant, System)
        public ChatRole Role { get; set; }                          
        // 대화 내용
        public string Content { get; set; } = string.Empty;          
        // 대화 발생 시각
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;   
        // 추가 메타데이터
        public Dictionary<string, string> Metadata { get; set; } = new(); 
        // 캐릭터 ID (AI 어시스턴트)
        public string? CharacterId { get; set; }                     
        // 사용자 ID
        public string? UserId { get; set; }                          
        // 삭제 여부
        public bool IsDeleted { get; set; } = false;                 
    }
} 