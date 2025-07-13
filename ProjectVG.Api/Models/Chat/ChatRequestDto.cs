using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Chat
{
    public class ChatRequestDto
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("actor")]
        public string Actor { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("character_id")]
        public Guid? CharacterId { get; set; }
    }
} 