using ProjectVG.Domain.Entities.Characters;

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
        /// Characters 엔티티로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="character">Characters 엔티티</param>
        public CharacterDto(ProjectVG.Domain.Entities.Characters.Character character)
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


    }
} 