using System.Text.Json.Serialization;
using ProjectVG.Application.Models.Character;

namespace ProjectVG.Api.Models.Character.Request
{
    public class UpdateCharacterRequest
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
        /// 현재 요청 모델의 필드(Name, Description, Role, IsActive)를 사용해 UpdateCharacterCommand 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <returns>요청 값으로 초기화된 <see cref="UpdateCharacterCommand"/> 객체.</returns>
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