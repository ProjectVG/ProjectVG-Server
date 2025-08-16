using ProjectVG.Domain.Entities.Characters;

namespace ProjectVG.Application.Models.Character
{
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
        /// 빈 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 생성자는 본문이 비어 있으며 각 속성의 기본값은 선언부의 초기화기를 통해 설정됩니다.
        /// </remarks>
        public CharacterDto()
        {
        }

        /// <summary>
        /// 도메인 Character 인스턴스의 속성값을 현재 DTO의 필드로 복사하여 초기화합니다.
        /// </summary>
        /// <param name="character">값을 복사할 도메인 엔티티(널이 아님). null이면 예기치 않은 NullReferenceException이 발생할 수 있습니다.</param>
        public CharacterDto(Domain.Entities.Characters.Character character)
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
