using System.Text.Json.Serialization;

namespace ProjectVG.Api.Models.Auth.Response
{
    public class AuthResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("user_id")]
        public Guid? UserId { get; set; }
        
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
