using System.Text.Json.Serialization;
using ProjectVG.Application.Models.User;
using ProjectVG.Domain.Entities.Users;


namespace ProjectVG.Api.Models.Auth.Request
{
    public class RegisterRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        

        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 현재 요청 데이터를 기반으로 새 사용자 전송 객체(UserDto)를 생성합니다.
        /// </summary>
        /// <returns>Username, Email을 복사하고 Provider는 "local", ProviderId는 Username으로 설정하며 Status는 AccountStatus.Active로 설정된 UserDto 인스턴스.</returns>
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
