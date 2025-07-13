using ProjectVG.Domain.Entities.User;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Repositories.InMemory
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = new();
        private readonly Dictionary<string, User> _usersByUsername = new();
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
            _usersByUsername.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public Task<User> CreateAsync(User user)
        {
            if (_usersByUsername.ContainsKey(user.Username))
            {
                throw new InvalidOperationException($"Username '{user.Username}' already exists.");
            }

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            
            _users[user.Id] = user;
            _usersByUsername[user.Username] = user;
            
            _logger.LogInformation("User created: {Username} with ID: {UserId}", user.Username, user.Id);
            
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                throw new KeyNotFoundException($"User with ID {user.Id} not found.");
            }

            var existingUser = _users[user.Id];
            
            // Username이 변경된 경우 중복 체크
            if (existingUser.Username != user.Username && _usersByUsername.ContainsKey(user.Username))
            {
                throw new InvalidOperationException($"Username '{user.Username}' already exists.");
            }

            // 기존 username 인덱스 제거
            _usersByUsername.Remove(existingUser.Username);
            
            user.UpdatedAt = DateTimeOffset.UtcNow;
            _users[user.Id] = user;
            _usersByUsername[user.Username] = user;
            
            _logger.LogInformation("User updated: {Username} with ID: {UserId}", user.Username, user.Id);
            
            return Task.FromResult(user);
        }

        public Task DeleteAsync(Guid id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                _users.Remove(id);
                _usersByUsername.Remove(user.Username);
                _logger.LogInformation("User deleted: {Username} with ID: {UserId}", user.Username, id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete user with ID: {UserId}, but it was not found", id);
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
                    Username = "admin",
                    Email = "admin@example.com",
                    DisplayName = "관리자",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    Email = "user1@example.com",
                    DisplayName = "테스트 사용자 1",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Id] = user;
                _usersByUsername[user.Username] = user;
            }

            _logger.LogInformation("Initialized {Count} default users", defaultUsers.Count);
        }
    }
} 