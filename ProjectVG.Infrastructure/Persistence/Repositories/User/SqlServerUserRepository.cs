using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Users
{
    public class SqlServerUserRepository : IUserRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerUserRepository> _logger;

        public SqlServerUserRepository(ProjectVGDbContext context, ILogger<SqlServerUserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<User?> GetByProviderIdAsync(string providerId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ProviderId == providerId && u.IsActive);
        }

        public async Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("사용자를 생성했습니다. 이름: {UserName}, ID: {UserId}", user.Name, user.Id);
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == user.Id && u.IsActive);

            if (existingUser == null)
            {
                throw new KeyNotFoundException($"ID {user.Id}인 사용자를 찾을 수 없습니다.");
            }

            existingUser.Name = user.Name;
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.ProviderId = user.ProviderId;
            existingUser.Provider = user.Provider;
            existingUser.IsActive = user.IsActive;
            existingUser.Update();

            await _context.SaveChangesAsync();

            _logger.LogInformation("사용자를 수정했습니다. 이름: {UserName}, ID: {UserId}", user.Name, user.Id);
            return existingUser;
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user != null)
            {
                user.IsActive = false;
                user.Update();
                await _context.SaveChangesAsync();

                _logger.LogInformation("사용자를 삭제했습니다. ID: {UserId}", id);
            }
            else
            {
                _logger.LogWarning("ID {UserId}인 사용자를 삭제하려 했지만 사용자를 찾을 수 없습니다", id);
            }
        }
    }
} 