using System.Text.Json.Serialization;
using ProjectVG.Application.Models.Character;

namespace ProjectVG.Api.Models.Character.Request
{
    public record UpdateCharacterRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; init; } = true;

        public UpdateCharacterCommand ToUpdateCharacterCommand()
        {
            return new UpdateCharacterCommand
            {
                Name = Name,
                Description = Description,
                Role = Role,
                IsActive = IsActive
            };
        }
    }
} 