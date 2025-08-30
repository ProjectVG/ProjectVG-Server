using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

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
                .Where(u => u.Status == AccountStatus.Active)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status != AccountStatus.Deleted);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Status != AccountStatus.Deleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status != AccountStatus.Deleted);
        }

        public async Task<User?> GetByProviderIdAsync(string providerId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ProviderId == providerId && u.Status != AccountStatus.Deleted);
        }

        public async Task<User?> GetByProviderAsync(string provider, string providerId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderId == providerId && u.Status != AccountStatus.Deleted);
        }

        public async Task<User?> GetByUIDAsync(string uid)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UID == uid && u.Status != AccountStatus.Deleted);
        }

        public async Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.Status = AccountStatus.Active; 

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id && u.Status != AccountStatus.Deleted);
            if (existingUser == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, "User", user.Id);
            }

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.UID = user.UID;
            existingUser.ProviderId = user.ProviderId;
            existingUser.Provider = user.Provider;
            existingUser.Status = user.Status;
            existingUser.Update();

            await _context.SaveChangesAsync();

            return existingUser;
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status != AccountStatus.Deleted);

            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, "User", id);
            }

            user.Status = AccountStatus.Deleted;
            user.Update();
            await _context.SaveChangesAsync();
        }
    }
}
