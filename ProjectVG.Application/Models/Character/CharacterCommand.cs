namespace ProjectVG.Application.Models.Character
{
    public record CharacterCommand
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
    }
}
