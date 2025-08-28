using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Characters
{
    /// <summary>
    /// AI 캐릭터
    /// </summary>
    public class Character : BaseEntity
    {
        /// <summary> 캐릭터 고유 ID </summary>
        public Guid Id { get; set; }
        
        /// <summary> 캐릭터 이름 </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary> 캐릭터+대화에 대한 간단한 요약 </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary> 캐릭터 설명 </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary> 캐릭터 역할/타입 </summary>
        public string Role { get; set; } = string.Empty;
        
        /// <summary> 캐릭터 성격 </summary>
        public string Personality { get; set; } = string.Empty;
        
        /// <summary> 캐릭터 말투/화법 </summary>
        public string SpeechStyle { get; set; } = string.Empty;

        /// <summary> 유저 별칭 (예: 주인님, 오너 등) </summary>
        public string UserAlias { get; set; } = string.Empty;

        /// <summary> 캐릭터 배경 </summary>
        public string Background { get; set; } = string.Empty;

        /// <summary> 캐릭터 이미지 URL </summary>
        public string ImageUrl { get; set; } = "";

        /// <summary> 활성화 여부 </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 캐릭터 보이스 ID
        /// </summary>
        public string VoiceId { get; set; } = string.Empty;
    }
} 