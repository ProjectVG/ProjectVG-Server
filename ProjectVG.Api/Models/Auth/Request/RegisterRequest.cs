using System.Text.Json.Serialization;
using ProjectVG.Application.Models.User;
using ProjectVG.Domain.Entities.Users;


namespace ProjectVG.Api.Models.Auth.Request
{
    public record RegisterRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; init; } = string.Empty;
        

        
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; init; } = string.Empty;

        public UserDto ToUserDto()
        {
            return new UserDto
            {
                Username = Username,
                Email = Email,
                Provider = "local",
                ProviderId = Username,
                Status = AccountStatus.Active
            };
        }
    }
}
