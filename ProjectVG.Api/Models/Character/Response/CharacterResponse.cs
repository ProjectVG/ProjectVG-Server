using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.Characters;
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

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; init; } = string.Empty;

        [JsonPropertyName("voice_id")]
        public string VoiceId { get; init; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; init; } = true;

        [JsonPropertyName("config_mode")]
        public string ConfigMode { get; init; } = "individual";

        [JsonPropertyName("individual_config")]
        public IndividualConfig? IndividualConfig { get; init; }

        [JsonPropertyName("system_prompt")]
        public string? SystemPrompt { get; init; }

        [JsonPropertyName("effective_system_prompt")]
        public string EffectiveSystemPrompt { get; init; } = string.Empty;

        [JsonPropertyName("created_by_user_id")]
        public Guid? CreatedByUserId { get; init; }

        [JsonPropertyName("created_by_username")]
        public string? CreatedByUsername { get; init; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; init; }

        public static CharacterResponse ToResponseDto(CharacterDto characterDto)
        {
            return new CharacterResponse
            {
                Id = characterDto.Id,
                Name = characterDto.Name,
                Description = characterDto.Description,
                ImageUrl = characterDto.ImageUrl,
                VoiceId = characterDto.VoiceId,
                IsActive = characterDto.IsActive,
                ConfigMode = characterDto.ConfigMode.ToString().ToLowerInvariant(),
                IndividualConfig = characterDto.IndividualConfig,
                SystemPrompt = characterDto.SystemPrompt,
                EffectiveSystemPrompt = characterDto.EffectiveSystemPrompt,
                CreatedByUserId = characterDto.CreatedByUserId,
                CreatedByUsername = characterDto.CreatedByUsername,
                IsPublic = characterDto.IsPublic
            };
        }
    }
} 