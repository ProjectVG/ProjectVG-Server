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

        [JsonPropertyName("requested_at")]
        public DateTime RequestedAt { get; set; }

        [JsonPropertyName("user_id")]
        public Guid UserId { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("instruction")]
        public string? Instruction { get; set; }

        [JsonPropertyName("use_tts")]
        public bool UseTTS { get; set; } = true;

        public ProcessChatCommand ToProcessChatCommand()
        {
            return new ProcessChatCommand
            {
                SessionId = this.SessionId,
                Message = this.Message,
                CharacterId = this.CharacterId,
                RequestedAt = this.RequestedAt,
                Action = this.Action,
                Instruction = this.Instruction,
                UserId = this.UserId,
                UseTTS = this.UseTTS
            };
        }
    }
}
