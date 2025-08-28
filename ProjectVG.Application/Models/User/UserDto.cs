using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Models.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UID { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public AccountStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserDto()
        {
        }

        /// <summary>
        /// 도메인 User 엔티티의 값을 사용해 UserDto를 초기화합니다.
        /// </summary>
        /// <param name="user">초기화에 사용할 도메인 User 엔티티(널이 아님). Id, UID, Username, Email, ProviderId, Provider, Status 값을 DTO에 복사합니다. CreatedAt/UpdatedAt는 복사하지 않습니다.</param>
        public UserDto(Domain.Entities.Users.User user)
        {
            Id = user.Id;
            UID = user.UID;
            Username = user.Username;
            Email = user.Email;
            ProviderId = user.ProviderId;
            Provider = user.Provider;
            Status = user.Status;
        }

        /// <summary>
        /// 현재 DTO를 기반으로 새로운 도메인 User 엔티티 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <returns>Id, UID, Username, Email, ProviderId, Provider, Status 필드가 복사된 새 <see cref="Domain.Entities.Users.User"/> 인스턴스. CreatedAt 및 UpdatedAt 필드는 설정되지 않습니다.</returns>
        public Domain.Entities.Users.User ToEntity()
        {
            return new Domain.Entities.Users.User {
                Id = Id,
                UID = UID,
                Username = Username,
                Email = Email,
                ProviderId = ProviderId,
                Provider = Provider,
                Status = Status,
            };
        }
    }
}
