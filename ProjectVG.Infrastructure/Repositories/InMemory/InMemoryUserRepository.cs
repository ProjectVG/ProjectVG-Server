using ProjectVG.Domain.Entities.User;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Repositories.InMemory
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = new();
        private readonly Dictionary<string, User> _usersByUsername = new();
        private readonly Dictionary<string, User> _usersByEmail = new();
        private readonly Dictionary<string, User> _usersByProviderId = new();
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

        public Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByProviderIdAsync(string providerId)
        {
            _usersByProviderId.TryGetValue(providerId, out var user);
            return Task.FromResult(user);
        }

        public Task<User> CreateAsync(User user)
        {
            if (_usersByUsername.ContainsKey(user.Username))
            {
                throw new InvalidOperationException($"사용자명 '{user.Username}'이 이미 존재합니다.");
            }

            if (_usersByEmail.ContainsKey(user.Email))
            {
                throw new InvalidOperationException($"이메일 '{user.Email}'이 이미 존재합니다.");
            }

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            
            _users[user.Id] = user;
            _usersByUsername[user.Username] = user;
            _usersByEmail[user.Email] = user;
            if (!string.IsNullOrEmpty(user.ProviderId))
            {
                _usersByProviderId[user.ProviderId] = user;
            }
            
            _logger.LogInformation("사용자를 생성했습니다. 사용자명: {Username}, ID: {UserId}", user.Username, user.Id);
            
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                throw new KeyNotFoundException($"ID {user.Id}인 사용자를 찾을 수 없습니다.");
            }

            var existingUser = _users[user.Id];
            
            // Username이 변경된 경우 중복 체크
            if (existingUser.Username != user.Username && _usersByUsername.ContainsKey(user.Username))
            {
                throw new InvalidOperationException($"사용자명 '{user.Username}'이 이미 존재합니다.");
            }

            // Email이 변경된 경우 중복 체크
            if (existingUser.Email != user.Email && _usersByEmail.ContainsKey(user.Email))
            {
                throw new InvalidOperationException($"이메일 '{user.Email}'이 이미 존재합니다.");
            }

            // 기존 인덱스 제거
            _usersByUsername.Remove(existingUser.Username);
            _usersByEmail.Remove(existingUser.Email);
            if (!string.IsNullOrEmpty(existingUser.ProviderId))
            {
                _usersByProviderId.Remove(existingUser.ProviderId);
            }
            
            user.UpdatedAt = DateTime.UtcNow;
            _users[user.Id] = user;
            _usersByUsername[user.Username] = user;
            _usersByEmail[user.Email] = user;
            if (!string.IsNullOrEmpty(user.ProviderId))
            {
                _usersByProviderId[user.ProviderId] = user;
            }
            
            _logger.LogInformation("사용자를 수정했습니다. 사용자명: {Username}, ID: {UserId}", user.Username, user.Id);
            
            return Task.FromResult(user);
        }

        public Task DeleteAsync(Guid id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                _users.Remove(id);
                _usersByUsername.Remove(user.Username);
                _usersByEmail.Remove(user.Email);
                if (!string.IsNullOrEmpty(user.ProviderId))
                {
                    _usersByProviderId.Remove(user.ProviderId);
                }
                _logger.LogInformation("사용자를 삭제했습니다. 사용자명: {Username}, ID: {UserId}", user.Username, id);
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
                    Username = "admin",
                    Name = "Administrator",
                    Email = "admin@projectvg.com",
                    Provider = "local",
                    ProviderId = "admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    Name = "Test User",
                    Email = "user1@projectvg.com",
                    Provider = "local",
                    ProviderId = "user1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            foreach (var user in defaultUsers)
            {
                _users[user.Id] = user;
                _usersByUsername[user.Username] = user;
                _usersByEmail[user.Email] = user;
                _usersByProviderId[user.ProviderId] = user;
            }

            _logger.LogInformation("기본 사용자 {Count}명을 초기화했습니다", defaultUsers.Count);
        }
    }
} 