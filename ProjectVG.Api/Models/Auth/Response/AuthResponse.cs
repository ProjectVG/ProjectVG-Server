using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Auth.Response
{
    public record AuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
        
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
        
        [JsonPropertyName("user_id")]
        public Guid? UserId { get; init; }
        
        [JsonPropertyName("username")]
        public string? Username { get; init; }
        
        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }
}
