using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Users
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = new();
        private readonly ILogger<InMemoryUserRepository> _logger;

        public InMemoryUserRepository(ILogger<InMemoryUserRepository> logger)
        {
            _logger = logger;
            InitializeDefaultUsers();
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return Task.FromResult(_users.Values.AsEnumerable());
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            var user = _users.Values.FirstOrDefault(u => u.Username == username);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email == email);
            return Task.FromResult(user);
        }

        public Task<User?> GetByProviderIdAsync(string providerId)
        {
            var user = _users.Values.FirstOrDefault(u => u.ProviderId == providerId);
            return Task.FromResult(user);
        }

        public Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
            
            _users[user.Id] = user;
            _logger.LogInformation("사용자를 생성했습니다. 이름: {UserName}, ID: {UserId}", user.Name, user.Id);
            
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                throw new KeyNotFoundException($"ID {user.Id}인 사용자를 찾을 수 없습니다.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            _users[user.Id] = user;
            _logger.LogInformation("사용자를 수정했습니다. 이름: {UserName}, ID: {UserId}", user.Name, user.Id);
            
            return Task.FromResult(user);
        }

        public Task DeleteAsync(Guid id)
        {
            if (_users.Remove(id))
            {
                _logger.LogInformation("사용자를 삭제했습니다. ID: {UserId}", id);
            }
            else
            {
                _logger.LogWarning("ID {UserId}인 사용자를 삭제하려 했지만 사용자를 찾을 수 없습니다", id);
            }
            
            return Task.CompletedTask;
        }

        private void InitializeDefaultUsers()
        {
            var defaultUsers = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "테스트 사용자 1",
                    Username = "testuser1",
                    Email = "test1@example.com",
                    Provider = "local",
                    ProviderId = "local_test1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "테스트 사용자 2",
                    Username = "testuser2",
                    Email = "test2@example.com",
                    Provider = "local",
                    ProviderId = "local_test2",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Id] = user;
            }
        }
    }
}
