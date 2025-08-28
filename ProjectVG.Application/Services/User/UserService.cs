using ProjectVG.Application.Models.User;
using ProjectVG.Common.Utils;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;

namespace ProjectVG.Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// UserService의 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// DI로 전달된 IUserRepository와 ILogger<UserService>를 내부 필드에 할당하여 서비스의 의존성을 설정합니다.
        /// </remarks>
        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// 새 사용자 계정을 생성하고 생성된 사용자의 DTO를 반환합니다.
        /// </summary>
        /// <param name="command">생성할 사용자의 정보(이메일, 사용자명, Provider 및 ProviderId 등)를 담은 명령 객체.</param>
        /// <returns>생성된 사용자 정보를 담은 <see cref="UserDto"/> 객체.</returns>
        /// <exception cref="ValidationException">이메일 또는 사용자명이 이미 존재할 경우 각각 <see cref="ErrorCode.EMAIL_ALREADY_EXISTS"/> 또는 <see cref="ErrorCode.USERNAME_ALREADY_EXISTS"/> 코드와 함께 throw됩니다.</exception>
        public async Task<UserDto> CreateUserAsync(UserCreateCommand command)
        {
            if (await ExistsByEmailAsync(command.Email))
                throw new ValidationException(ErrorCode.EMAIL_ALREADY_EXISTS, command.Email);

            if (await ExistsByUsernameAsync(command.Username))
                throw new ValidationException(ErrorCode.USERNAME_ALREADY_EXISTS, command.Username);

            var user = new User {
                Id = Guid.NewGuid(),
                UID = await GenerateUniqueUIDAsync(),
                Username = command.Username,
                Email = command.Email,
                Provider = command.Provider,
                ProviderId = command.ProviderId,
                Status = AccountStatus.Active
            };

            var created = await _userRepository.CreateAsync(user);

            _logger.LogInformation("사용자 생성 완료: ID {UserId}, UID {UID}, 사용자명 {Username}",
                created.Id, created.UID, created.Username);

            return new UserDto(created);
        }

        /// <summary>
        /// 사용자를 소프트 삭제(계정 상태를 Deleted로 변경)하고 변경사항을 저장합니다.
        /// </summary>
        /// <param name="userId">삭제할 사용자의 식별자(Guid).</param>
        /// <returns>삭제 작업이 성공하면 true를 반환합니다.</returns>
        /// <exception cref="NotFoundException">해당 ID의 사용자를 찾을 수 없는 경우 발생합니다.</exception>
        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, userId);

            user.Status = AccountStatus.Deleted;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("사용자 삭제 완료: ID {UserId}, 사용자명 {Username}", userId, user.Username);
            return true;
        }

        /// <summary>
        /// 지정된 ID의 사용자를 조회하여 UserDto를 반환합니다.
        /// </summary>
        /// <param name="userId">조회할 사용자의 GUID 식별자.</param>
        /// <returns>사용자를 찾으면 해당 사용자의 UserDto, 찾지 못하면 null을 반환합니다.</returns>
        public async Task<UserDto?> TryGetByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user is null ? null : new UserDto(user);
        }

        /// <summary>
        /// 주어진 UID로 사용자를 조회하여 UserDto를 반환합니다. UID에 해당하는 사용자가 없으면 null을 반환합니다.
        /// </summary>
        /// <param name="uid">조회할 사용자의 고유 식별자(UID).</param>
        /// <returns>해당 UID의 사용자를 나타내는 UserDto 인스턴스 또는 사용자가 없으면 null.</returns>
        public async Task<UserDto?> TryGetByUidAsync(string uid)
        {
            var user = await _userRepository.GetByUIDAsync(uid);
            return user is null ? null : new UserDto(user);
        }

        /// <summary>
        /// 사용자명(username)으로 사용자를 조회하여 UserDto를 반환합니다.
        /// </summary>
        /// <param name="username">조회할 사용자의 사용자명.</param>
        /// <returns>사용자가 존재하면 해당 UserDto, 존재하지 않으면 null.</returns>
        public async Task<UserDto?> TryGetByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user is null ? null : new UserDto(user);
        }

        /// <summary>
        /// 지정된 외부 인증 공급자(provider)와 공급자 식별자(providerId)에 대응하는 사용자를 조회하여 UserDto를 반환합니다.
        /// </summary>
        /// <param name="provider">외부 인증 공급자 이름(예: "google", "github").</param>
        /// <param name="providerId">해당 공급자에서 발급된 사용자의 고유 식별자.</param>
        /// <returns>일치하는 사용자가 있으면 해당 사용자의 UserDto, 없으면 null.</returns>
        public async Task<UserDto?> TryGetByProviderAsync(string provider, string providerId)
        {
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Provider == provider && u.ProviderId == providerId);
            return user is null ? null : new UserDto(user);
        }

        /// <summary>
/// 주어진 이메일을 가진 사용자가 존재하는지 여부를 비동기적으로 확인합니다.
/// </summary>
/// <param name="email">조회할 사용자의 이메일 주소.</param>
/// <returns>해당 이메일을 가진 사용자가 존재하면 <c>true</c>, 그렇지 않으면 <c>false</c>를 반환하는 비동기 작업.</returns>
public Task<bool> ExistsByEmailAsync(string email) => ExistsAsync(() => _userRepository.GetByEmailAsync(email));
        /// <summary>
/// 지정한 사용자 이름(username)을 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <returns>해당 사용자 이름이 존재하면 true, 아니면 false를 반환하는 Task.</returns>
public Task<bool> ExistsByUsernameAsync(string username) => ExistsAsync(() => _userRepository.GetByUsernameAsync(username));
        /// <summary>
/// 지정한 사용자 ID를 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="userId">확인할 사용자의 GUID 식별자.</param>
/// <returns>해당 ID를 가진 사용자가 존재하면 true, 없으면 false를 반환하는 Task.</returns>
public Task<bool> ExistsByIdAsync(Guid userId) => ExistsAsync(() => _userRepository.GetByIdAsync(userId));
        /// <summary>
/// 지정된 UID를 가진 사용자가 존재하는지 비동기적으로 검사합니다.
/// </summary>
/// <param name="uid">확인할 사용자의 고유 식별자(UID).</param>
/// <returns>해당 UID를 가진 사용자가 존재하면 true, 그렇지 않으면 false를 반환하는 Task.</returns>
public Task<bool> ExistsByUidAsync(string uid) => ExistsAsync(() => _userRepository.GetByUIDAsync(uid));

        /// <summary>
            /// 지정된 비동기 조회 함수를 실행하여 결과가 null이 아닌지 확인하고, null이 아니면 true를 반환합니다.
            /// </summary>
            /// <param name="getter">존재 여부를 판별할 객체를 비동기로 반환하는 함수(Task&lt;T?&gt;).</param>
            /// <returns>조회 결과가 null이 아니면 true, null이면 false.</returns>
            private static async Task<bool> ExistsAsync<T>(Func<Task<T?>> getter) where T : class
            => await getter() is not null;

        /// <summary>
        /// 새 사용자에 사용할 고유한 UID를 생성하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// 랜덤 UID를 생성한 뒤 저장소에 이미 존재하는지 검사하여 중복이 없을 때까지 반복합니다.
        /// 최대 10회 시도 후에도 고유한 UID를 찾지 못하면 예외를 던집니다.
        /// </remarks>
        /// <returns>중복되지 않는 UID 문자열.</returns>
        /// <exception cref="InvalidOperationException">최대 허용 시도 횟수(10회)를 초과한 경우.</exception>
        private async Task<string> GenerateUniqueUIDAsync()
        {
            string uid;
            int attempts = 0;
            const int maxAttempts = 10;
            do {
                uid = UidGenerator.GenerateRandomUID(); attempts++; 
                if (attempts > maxAttempts) { 
                    throw new InvalidOperationException("UID 생성 시도 횟수 초과"); 
                }
            } while (await ExistsByUidAsync(uid)); return uid;
        }
    }

}
