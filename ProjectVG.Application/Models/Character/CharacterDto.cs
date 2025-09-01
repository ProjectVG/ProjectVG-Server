using ProjectVG.Domain.Entities.Characters;

namespace ProjectVG.Application.Models.Character
{
    public record CharacterDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string VoiceId { get; init; } = string.Empty;
        
        public CharacterConfigMode ConfigMode { get; init; }
        public IndividualConfig? IndividualConfig { get; init; }
        public string? SystemPrompt { get; init; }
        
        public string EffectiveSystemPrompt { get; init; } = string.Empty;

        public CharacterDto(Domain.Entities.Characters.Character character)
        {
            Id = character.Id;
            Name = character.Name;
            Description = character.Description;
            ImageUrl = character.ImageUrl;
            IsActive = character.IsActive;
            VoiceId = character.VoiceId;
            ConfigMode = character.ConfigMode;
            IndividualConfig = character.IndividualConfig;
            SystemPrompt = character.SystemPrompt;
            EffectiveSystemPrompt = character.GetEffectiveSystemPrompt();
        }
    }
}
