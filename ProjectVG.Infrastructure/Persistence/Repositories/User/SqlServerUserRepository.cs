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

        /// <summary>
        /// 활성 상태(AccountStatus.Active)인 사용자들을 사용자명(Username) 오름차순으로 정렬하여 비동기적으로 조회합니다.
        /// </summary>
        /// <returns>활성 사용자 목록을 비동기적으로 반환합니다 (IEnumerable&lt;User&gt;).</returns>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.Status == AccountStatus.Active)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        /// <summary>
        /// 지정된 식별자에 해당하는 삭제되지 않은 사용자 엔터티를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="id">조회할 사용자의 고유 식별자(Guid).</param>
        /// <returns>조회된 User 객체(Task 결과). 해당 사용자를 찾지 못했거나 삭제된 경우 null.</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status != AccountStatus.Deleted);
        }

        /// <summary>
        /// 지정한 사용자 이름(username)에 해당하는 활성(삭제되지 않은) 사용자를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="username">조회할 사용자의 사용자 이름(Username).</param>
        /// <returns>일치하는 사용자가 있으면 해당 User 객체를, 없으면 null을 반환합니다. 삭제된(Status == AccountStatus.Deleted) 사용자는 조회 대상에서 제외됩니다.</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Status != AccountStatus.Deleted);
        }

        /// <summary>
        /// 지정한 이메일과 일치하는 삭제되지 않은 사용자 엔터티를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="email">조회할 이메일 문자열(정확 일치, 대소문자 비교는 DB의 설정에 따름).</param>
        /// <returns>일치하는 사용자가 있으면 해당 <see cref="User"/> 객체, 없으면 <c>null</c>.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status != AccountStatus.Deleted);
        }

        /// <summary>
        /// 지정된 외부 제공자 ID에 해당하는 활성(삭제되지 않은) 사용자를 비동기적으로 검색합니다.
        /// </summary>
        /// <param name="providerId">검색할 사용자의 외부 제공자 식별자(provider ID).</param>
        /// <returns>일치하는 사용자가 있으면 해당 User 객체를, 없으면 null을 반환합니다.</returns>
        public async Task<User?> GetByProviderIdAsync(string providerId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ProviderId == providerId && u.Status != AccountStatus.Deleted);
        }

        /// <summary>
        — 지정된 UID(사용자 고유 식별자)를 가진, 삭제 상태가 아닌 사용자 엔터티를 비동기적으로 조회합니다.
        /// </summary>
        /// <param name="uid">조회할 사용자의 UID(고유 식별자).</param>
        /// <returns>일치하는 사용자가 있으면 해당 <see cref="User"/> 객체, 없으면 null을 반환합니다.</returns>
        public async Task<User?> GetByUIDAsync(string uid)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UID == uid && u.Status != AccountStatus.Deleted);
        }

        /// <summary>
        /// 새 사용자 엔티티를 생성하여 영구 저장소에 저장하고 생성된 사용자 객체를 반환합니다.
        /// </summary>
        /// <param name="user">생성할 사용자 엔티티(메서드에서 Id, CreatedAt, UpdatedAt, Status가 설정됩니다).</param>
        /// <returns>저장된 사용자 엔티티(데이터베이스에 반영된 상태).</returns>
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

        /// <summary>
        /// 지정한 사용자 엔티티의 정보를 갱신하고 갱신된 엔티티를 반환합니다.
        /// </summary>
        /// <param name="user">갱신할 값을 가진 사용자 엔티티(유효한 Id를 포함해야 함).</param>
        /// <returns>데이터베이스에 저장된 갱신된 사용자 엔티티.</returns>
        /// <exception cref="NotFoundException">요청된 Id의 사용자가 존재하지 않거나 삭제된 상태일 경우 발생합니다.</exception>
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

        /// <summary>
        /// 지정한 사용자를 소프트 삭제(상태를 AccountStatus.Deleted로 설정)하고 변경 사항을 영속화합니다.
        /// </summary>
        /// <param name="id">삭제할 사용자의 식별자(Guid).</param>
        /// <exception cref="NotFoundException">지정한 Id에 해당하는 활성(삭제되지 않은) 사용자가 존재하지 않을 경우 발생합니다.</exception>
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
