using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Models.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 빈 UserDto 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 모든 문자열 속성은 빈 문자열로, 기타 값 형식 속성은 각 타입의 기본값으로 초기화됩니다.
        /// </remarks>
        public UserDto()
        {
        }

        /// <summary>
        /// 도메인 User 엔티티의 값을 사용해 UserDto 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="user">초기화에 사용할 도메인 User 엔티티. Id, Username, Name, Email, ProviderId, Provider, IsActive 값을 복사합니다(생성/수정일은 매핑되지 않음).</param>
        public UserDto(Domain.Entities.Users.User user)
        {
            Id = user.Id;
            Username = user.Username;
            Name = user.Name;
            Email = user.Email;
            ProviderId = user.ProviderId;
            Provider = user.Provider;
            IsActive = user.IsActive;
        }

        /// <summary>
        /// DTO의 값을 사용해 새로운 <see cref="Domain.Entities.Users.User"/> 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// 생성되는 도메인 객체는 Id, Username, Name, Email, ProviderId, Provider, IsActive 속성만 DTO의 값으로 초기화합니다.
        /// CreatedAt 및 UpdatedAt 등 타임스탬프 필드는 설정되지 않습니다.
        /// </remarks>
        /// <returns>DTO 데이터를 복사한 새로운 <see cref="Domain.Entities.Users.User"/> 인스턴스.</returns>
        public Domain.Entities.Users.User ToEntity()
        {
            return new Domain.Entities.Users.User {
                Id = Id,
                Username = Username,
                Name = Name,
                Email = Email,
                ProviderId = ProviderId,
                Provider = Provider,
                IsActive = IsActive,
            };
        }
    }
}
