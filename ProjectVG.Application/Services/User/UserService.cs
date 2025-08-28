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

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

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

        public async Task<UserDto?> TryGetByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user is null ? null : new UserDto(user);
        }

        public async Task<UserDto?> TryGetByUidAsync(string uid)
        {
            var user = await _userRepository.GetByUIDAsync(uid);
            return user is null ? null : new UserDto(user);
        }

        public async Task<UserDto?> TryGetByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user is null ? null : new UserDto(user);
        }

        public async Task<UserDto?> TryGetByProviderAsync(string provider, string providerId)
        {
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Provider == provider && u.ProviderId == providerId);
            return user is null ? null : new UserDto(user);
        }

        public Task<bool> ExistsByEmailAsync(string email) => ExistsAsync(() => _userRepository.GetByEmailAsync(email));
        public Task<bool> ExistsByUsernameAsync(string username) => ExistsAsync(() => _userRepository.GetByUsernameAsync(username));
        public Task<bool> ExistsByIdAsync(Guid userId) => ExistsAsync(() => _userRepository.GetByIdAsync(userId));
        public Task<bool> ExistsByUidAsync(string uid) => ExistsAsync(() => _userRepository.GetByUIDAsync(uid));

        private static async Task<bool> ExistsAsync<T>(Func<Task<T?>> getter) where T : class
            => await getter() is not null;

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
