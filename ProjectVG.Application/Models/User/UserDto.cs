using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Models.User
{
    public record UserDto
    {
        public Guid Id { get; init; }
        public string UID { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string ProviderId { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public AccountStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public UserDto()
        {
            Id = Guid.NewGuid();
            UID = string.Empty;
            Username = string.Empty;
            Email = string.Empty;
            ProviderId = string.Empty;
            Provider = string.Empty;
            Status = AccountStatus.Active;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
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
