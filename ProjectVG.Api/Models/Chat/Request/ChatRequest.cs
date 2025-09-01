using System.Text.Json.Serialization;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Models.API.Request
{
    public record ChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("character_id")]
        public Guid CharacterId { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }

        [JsonPropertyName("use_tts")]
        public bool UseTTS { get; init; } = true;

        [JsonPropertyName("request_at")]
        public DateTime RequestAt { get; init; }
    }
}
