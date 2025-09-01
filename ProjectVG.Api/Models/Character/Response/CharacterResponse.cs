using ProjectVG.Application.Models.Character;
using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Character.Response
{
    public record CharacterResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; init; } = true;


        public static CharacterResponse ToResponseDto(CharacterDto characterDto)
        {
            return new CharacterResponse {
                Id = characterDto.Id,
                Name = characterDto.Name,
                Description = characterDto.Description,
                Role = characterDto.Role,
                IsActive = characterDto.IsActive
            };
        }
    }
} 