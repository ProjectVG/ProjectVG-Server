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
            if (_usersByUsername.ContainsKey(user.Name))
            {
                throw new InvalidOperationException($"Username '{user.Name}' already exists.");
            }

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            
            _users[user.Id] = user;
            _usersByUsername[user.Name] = user;
            
            _logger.LogInformation("User created: {Username} with ID: {UserId}", user.Name, user.Id);
            
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
            if (existingUser.Name != user.Name && _usersByUsername.ContainsKey(user.Name))
            {
                throw new InvalidOperationException($"Username '{user.Name}' already exists.");
            }

            // 기존 username 인덱스 제거
            _usersByUsername.Remove(existingUser.Name);
            
            user.UpdatedAt = DateTime.UtcNow;
            _users[user.Id] = user;
            _usersByUsername[user.Name] = user;
            
            _logger.LogInformation("User updated: {Username} with ID: {UserId}", user.Name, user.Id);
            
            return Task.FromResult(user);
        }

        public Task DeleteAsync(Guid id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                _users.Remove(id);
                _usersByUsername.Remove(user.Name);
                _logger.LogInformation("User deleted: {Username} with ID: {UserId}", user.Name, id);
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
                    Name = "admin",
                    Email = "admin@example.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Name = "user1",
                    Email = "user1@example.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Id] = user;
                _usersByUsername[user.Name] = user;
            }

            _logger.LogInformation("Initialized {Count} default users", defaultUsers.Count);
        }
    }
} 