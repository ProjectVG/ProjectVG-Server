using ProjectVG.Domain.Entities.User;

namespace ProjectVG.Application.Models.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public string? Provider { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserDto()
        {
        }

        public UserDto(ProjectVG.Domain.Entities.User.User user)
        {
            Id = user.Id;
            Username = user.Username;
            Name = user.Name;
            Email = user.Email;
            ProviderId = user.ProviderId;
            Provider = user.Provider;
            IsActive = user.IsActive;
        }

        public ProjectVG.Domain.Entities.User.User ToEntity()
        {
            return new ProjectVG.Domain.Entities.User.User {
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