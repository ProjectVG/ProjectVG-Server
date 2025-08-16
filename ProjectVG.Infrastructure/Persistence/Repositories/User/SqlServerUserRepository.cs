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
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 지정된 식별자(Id)를 가진 활성 사용자(User)를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="id">조회할 사용자의 고유 식별자(Guid).</param>
        /// <returns>일치하는 활성 사용자가 있으면 해당 User 객체를, 없으면 null을 반환하는 비동기 작업(Task&lt;User?&gt;).</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        /// <summary>
        /// 지정된 사용자 이름과 정확히 일치하는 활성 사용자(User)를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="username">조회할 사용자명(정확 일치 기준).</param>
        /// <returns>일치하는 활성 사용자가 있으면 해당 User 객체, 없으면 null.</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        /// <summary>
        /// 지정된 이메일을 가진 활성 사용자(User)를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="email">검색할 사용자의 이메일 주소.</param>
        /// <returns>일치하는 활성 사용자가 있으면 해당 User 객체, 없으면 null.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        /// <summary>
        /// 주어진 ProviderId와 일치하며 활성 상태(IsActive)인 사용자(User)를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="providerId">외부 인증 공급자에서 발급된 사용자 식별자(예: OAuth 제공자 ID).</param>
        /// <returns>일치하는 활성 사용자를 반환합니다. 찾지 못하면 null을 반환합니다.</returns>
        public async Task<User?> GetByProviderIdAsync(string providerId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ProviderId == providerId && u.IsActive);
        }

        /// <summary>
        /// 새 사용자 엔터티를 초기화하고 저장소에 추가한 뒤 저장된 사용자 객체를 반환합니다.
        /// </summary>
        /// <param name="user">저장할 사용자 엔터티(메서드가 Id, CreatedAt, UpdatedAt, IsActive 필드를 설정함).</param>
        /// <returns>데이터베이스에 저장되어 Id와 타임스탬프가 설정된 User 객체.</returns>
        public async Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 활성 상태인 기존 사용자를 업데이트하고 변경 사항을 영구 저장한 뒤 갱신된 엔티티를 반환합니다.
        /// </summary>
        /// <param name="user">업데이트할 사용자 정보를 포함한 엔티티(식별자(Id) 는 기존 사용자와 일치해야 함).</param>
        /// <returns>저장된 변경 사항이 반영된 사용자 엔티티.</returns>
        /// <exception cref="NotFoundException">지정한 Id를 가진 활성 사용자(IsActive)가 존재하지 않을 경우 발생합니다.</exception>
        public async Task<User> UpdateAsync(User user)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id && u.IsActive);
            if (existingUser == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, "User", user.Id);
            }

            existingUser.Name = user.Name;
            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.ProviderId = user.ProviderId;
            existingUser.Provider = user.Provider;
            existingUser.IsActive = user.IsActive;
            existingUser.Update();

            await _context.SaveChangesAsync();

            return existingUser;
        }

        /// <summary>
        /// 지정한 ID의 활성 사용자를 소프트 삭제(비활성화)합니다.
        /// </summary>
        /// <param name="id">삭제할 사용자의 식별자(Guid).</param>
        /// <remarks>
        /// 해당 사용자의 IsActive를 false로 설정하고 엔티티의 Update()를 호출하여 변경 시간을 갱신한 뒤 변경 사항을 영구 저장합니다.
        /// </remarks>
        /// <exception cref="ProjectVG.Common.Exceptions.NotFoundException">지정한 ID의 활성 사용자가 존재하지 않는 경우 발생합니다. (ErrorCode.USER_NOT_FOUND)</exception>
        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, "User", id);
            }

            user.IsActive = false;
            user.Update();
            await _context.SaveChangesAsync();
        }
    }
}
