using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Characters
{
    /// <summary>
    /// AI 캐릭터 엔티티
    /// 
    /// 캐릭터 정보:
    /// - 기본 정보: 이름, 설명, 역할
    /// - 성격 설정: 성격, 말투, 배경
    /// - 메타데이터: 유동적 속성 (나이, 키, 취미 등)
    /// </summary>
    public class Character : BaseEntity
    {
        /// <summary>
        /// 캐릭터 고유 ID
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// 캐릭터 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 캐릭터 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 캐릭터 역할/타입
        /// </summary>
        public string Role { get; set; } = string.Empty;
        
        /// <summary>
        /// 캐릭터 성격
        /// </summary>
        public string Personality { get; set; } = string.Empty;
        
        /// <summary>
        /// 캐릭터 말투/화법
        /// </summary>
        public string SpeechStyle { get; set; } = string.Empty;
        
        /// <summary>
        /// 캐릭터 배경
        /// </summary>
        public string Background { get; set; } = string.Empty;
        
        /// <summary>
        /// 활성화 여부
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 유동적 메타데이터 (나이, 키, 취미 등)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
        
        /// <summary>
        /// 캐릭터 보이스 ID
        /// </summary>
        public string VoiceId { get; set; } = string.Empty;
    }
} 