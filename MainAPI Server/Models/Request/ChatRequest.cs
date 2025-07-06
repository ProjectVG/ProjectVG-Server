
using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.Request
{
    public class ChatRequest
    {

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }
}
