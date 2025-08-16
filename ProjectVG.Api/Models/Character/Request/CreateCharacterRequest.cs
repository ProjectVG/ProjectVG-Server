using System.Text.Json.Serialization;
using ProjectVG.Application.Models.Character;

namespace ProjectVG.Api.Models.Character.Request
{
    public class CreateCharacterRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 요청 모델의 값을 사용해 CreateCharacterCommand 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <returns>이 요청의 Name, Description, Role, IsActive 값을 복사한 CreateCharacterCommand 객체.</returns>
        public CreateCharacterCommand ToCreateCharacterCommand()
        {
            return new CreateCharacterCommand
            {
                Name = Name,
                Description = Description,
                Role = Role,
                IsActive = IsActive
            };
        }
    }
} 