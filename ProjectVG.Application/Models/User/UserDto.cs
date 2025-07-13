using ProjectVG.Domain.Entities.User;

namespace ProjectVG.Application.Models.User
{
    /// <summary>
    /// 사용자 데이터 전송 객체 (내부 비즈니스 로직용)
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public UserDto()
        {
        }

        /// <summary>
        /// User 엔티티로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="user">User 엔티티</param>
        public UserDto(ProjectVG.Domain.Entities.User.User user)
        {
            Id = user.Id;
            ProviderId = user.ProviderId;
            Provider = user.Provider;
            Email = user.Email;
            Name = user.Name;
            Username = user.Username;
            IsActive = user.IsActive;
            CreatedAt = user.CreatedAt;
        }

        /// <summary>
        /// DTO를 User 엔티티로 변환
        /// </summary>
        /// <returns>User 엔티티</returns>
        public ProjectVG.Domain.Entities.User.User ToUser()
        {
            return new ProjectVG.Domain.Entities.User.User
            {
                Id = Id,
                ProviderId = ProviderId,
                Provider = Provider,
                Email = Email,
                Name = Name,
                Username = Username,
                IsActive = IsActive,
                CreatedAt = CreatedAt
            };
        }
    }
} 