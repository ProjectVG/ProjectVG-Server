using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Auth.Request
{
    public record LoginRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; init; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; init; } = string.Empty;
    }
}
