using ProjectVG.Application.Models.Character;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Character.Request
{
    /// <summary>
    /// SystemPrompt로 캐릭터를 생성하는 요청
    /// </summary>
    public record CreateCharacterWithSystemPromptRequest
    {
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "캐릭터 이름은 필수입니다.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "캐릭터 이름은 1-100자 사이여야 합니다.")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        [StringLength(1000, ErrorMessage = "설명은 최대 1000자까지 입력 가능합니다.")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("image_url")]
        [StringLength(500, ErrorMessage = "이미지 URL은 최대 500자까지 입력 가능합니다.")]
        public string ImageUrl { get; init; } = string.Empty;

        [JsonPropertyName("voice_id")]
        [StringLength(100, ErrorMessage = "음성 ID는 최대 100자까지 입력 가능합니다.")]
        public string VoiceId { get; init; } = string.Empty;

        [JsonPropertyName("system_prompt")]
        [Required(ErrorMessage = "SystemPrompt는 필수입니다.")]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "SystemPrompt는 1-5000자 사이여야 합니다.")]
        public string SystemPrompt { get; init; } = string.Empty;

        public CreateCharacterWithSystemPromptCommand ToCommand(Guid? userId = null)
        {
            return new CreateCharacterWithSystemPromptCommand
            {
                Name = Name,
                Description = Description,
                ImageUrl = ImageUrl,
                VoiceId = VoiceId,
                SystemPrompt = SystemPrompt,
                UserId = userId
            };
        }
    }
}