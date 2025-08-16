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

        /// <summary>
        /// 현재 요청 정보를 기반으로 UserDto 객체를 생성하여 반환합니다.
        /// </summary>
        /// <returns>Username, Name, Email은 요청값을 그대로 복사하고 Provider는 "local", ProviderId는 Username, IsActive는 true로 설정된 새로운 <see cref="UserDto"/> 인스턴스. (비밀번호는 포함되지 않습니다.)</returns>
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
