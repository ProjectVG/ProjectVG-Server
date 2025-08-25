using System.Text.Json.Serialization;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Models.API.Request
{
    public class ChatRequest
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("character_id")]
        public Guid CharacterId { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("use_tts")]
        public bool UseTTS { get; set; } = true;
    }
}
