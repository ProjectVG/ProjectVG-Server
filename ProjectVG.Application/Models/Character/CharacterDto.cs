using ProjectVG.Domain.Entities.Characters;

namespace ProjectVG.Application.Models.Character
{
    public record CharacterDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string Personality { get; init; } = string.Empty;
        public string SpeechStyle { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string UserAlias { get; init; } = string.Empty;
        public string VoiceId { get; init; } = string.Empty;


        public CharacterDto(Domain.Entities.Characters.Character character)
        {
            Id = character.Id;
            Name = character.Name;
            Description = character.Description;
            Role = character.Role;            
            IsActive = character.IsActive;
            Personality = character.Personality;
            SpeechStyle = character.SpeechStyle;
            UserAlias = character.UserAlias;
            Summary = character.Summary;
            VoiceId = character.VoiceId;
        }
    }
}
