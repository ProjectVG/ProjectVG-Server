using ProjectVG.Domain.Entities.Character;

namespace ProjectVG.Application.Models.Character
{
    /// <summary>
    /// 캐릭터 데이터 전송 객체 (내부 비즈니스 로직용)
    /// </summary>
    public class CharacterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Personality { get; set; } = string.Empty;
        public string SpeechStyle { get; set; } = string.Empty;
        public string VoiceId { get; set; } = string.Empty;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public CharacterDto()
        {
        }

        /// <summary>
        /// Character 엔티티로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="character">Character 엔티티</param>
        public CharacterDto(ProjectVG.Domain.Entities.Character.Character character)
        {
            Id = character.Id;
            Name = character.Name;
            Description = character.Description;
            Role = character.Role;            
            IsActive = character.IsActive;
            Personality = character.Personality;
            SpeechStyle = character.SpeechStyle;
            VoiceId = character.VoiceId;
        }

        /// <summary>
        /// DTO를 Character 엔티티로 변환
        /// </summary>
        /// <returns>Character 엔티티</returns>
        public ProjectVG.Domain.Entities.Character.Character ToCharacter()
        {
            return new ProjectVG.Domain.Entities.Character.Character
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Role = Role,
                IsActive = IsActive,
                Personality = Personality,
                SpeechStyle = SpeechStyle,
                VoiceId = VoiceId
            };
        }
    }
} 