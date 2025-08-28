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
