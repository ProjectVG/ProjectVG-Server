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


        /// <summary>
        /// CharacterDto를 CharacterResponse로 변환하여 반환합니다.
        /// </summary>
        /// <param name="characterDto">변환할 소스 DTO. null이 아니어야 합니다.</param>
        /// <returns>소스 DTO의 필드(Id, Name, Description, Role, IsActive)를 복사한 새 CharacterResponse 인스턴스.</returns>
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