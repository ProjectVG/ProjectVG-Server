using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Auth.Response
{
    public record CheckResponse
    {
        [JsonPropertyName("exists")]
        public bool Exists { get; init; }
        
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
