
using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.Reponse
{
    public class ChatResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; }

        [JsonPropertyName("response")]
        public string Response { get; set; }
    }
}
