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

        public CharacterDto()
        {
        }

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
