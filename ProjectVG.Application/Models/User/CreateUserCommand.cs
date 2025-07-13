namespace ProjectVG.Application.Models.User
{
    /// <summary>
    /// 사용자 생성 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class CreateUserCommand
    {
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
} 