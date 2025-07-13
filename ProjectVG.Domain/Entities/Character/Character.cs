using System.Collections.Generic;
using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Character
{

    /// <summary>
    /// 캐릭터
    /// </summary>
    public class Character : BaseEntity
    {
        // 캐릭터 고유 ID
        public Guid Id { get; set; }
        // 캐릭터 이름
        public string Name { get; set; } = string.Empty;
        // 캐릭터 설명
        public string Description { get; set; } = string.Empty;
        // 캐릭터 역할/타입
        public string Role { get; set; } = string.Empty;
        // 캐릭터 성격
        public string Personality { get; set; } = string.Empty;
        // 캐릭터 배경
        public string Background { get; set; } = string.Empty;
        // 활성화 여부
        public bool IsActive { get; set; } = true;
        // 유동적 메타데이터 (나이, 키, 취미 등)
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
} 