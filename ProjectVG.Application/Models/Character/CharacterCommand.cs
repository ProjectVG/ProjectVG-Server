using ProjectVG.Domain.Entities.Characters;

namespace ProjectVG.Application.Models.Character
{
    /// <summary>
    /// 기본 캐릭터 생성 커맨드
    /// </summary>
    public abstract record CharacterCommand
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string VoiceId { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public Guid? UserId { get; init; }
    }
    
    /// <summary>
    /// 개별 설정 모드로 캐릭터 생성
    /// </summary>
    public record CreateCharacterWithFieldsCommand : CharacterCommand
    {
        public IndividualConfig IndividualConfig { get; init; } = new();
    }
    
    /// <summary>
    /// SystemPrompt 모드로 캐릭터 생성
    /// </summary>
    public record CreateCharacterWithSystemPromptCommand : CharacterCommand
    {
        public string SystemPrompt { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// 개별 설정 모드로 캐릭터 업데이트
    /// </summary>
    public record UpdateCharacterToIndividualCommand
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string VoiceId { get; init; } = string.Empty;
        public IndividualConfig IndividualConfig { get; init; } = new();
    }
    
    /// <summary>
    /// SystemPrompt 모드로 캐릭터 업데이트
    /// </summary>
    public record UpdateCharacterToSystemPromptCommand
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public string VoiceId { get; init; } = string.Empty;
        public string SystemPrompt { get; init; } = string.Empty;
    }
}
