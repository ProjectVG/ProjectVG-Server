using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Users
{
    public class User : BaseEntity
    {
        // 사용자 고유 ID
        public Guid Id { get; set; }
        // OAuth2 Provider ID
        public string ProviderId { get; set; } = string.Empty;
        // OAuth2 Provider
        public string Provider { get; set; } = string.Empty;
        // 사용자 이메일
        public string Email { get; set; } = string.Empty;
        // 사용자 이름
        public string Name { get; set; } = string.Empty;
        // 사용자명
        public string Username { get; set; } = string.Empty;
        // 활성화 여부
        public bool IsActive { get; set; } = true;
    }
} 