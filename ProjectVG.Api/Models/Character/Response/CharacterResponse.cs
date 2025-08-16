using ProjectVG.Application.Models.Character;
using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Character.Response
{
    public class CharacterResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;


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