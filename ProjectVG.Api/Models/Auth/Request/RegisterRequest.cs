using System.Text.Json.Serialization;
using ProjectVG.Application.Models.User;

namespace ProjectVG.Api.Models.Auth.Request
{
    public class RegisterRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        public UserDto ToUserDto()
        {
            return new UserDto
            {
                Username = Username,
                Name = Name,
                Email = Email,
                Provider = "local",
                ProviderId = Username,
                IsActive = true
            };
        }
    }
}
