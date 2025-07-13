namespace ProjectVG.Application.Models.User
{
    /// <summary>
    /// 사용자 수정 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class UpdateUserCommand
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
} 