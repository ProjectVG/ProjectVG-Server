using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Auth.Response
{
    public class CheckResponse
    {
        [JsonPropertyName("exists")]
        public bool Exists { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
