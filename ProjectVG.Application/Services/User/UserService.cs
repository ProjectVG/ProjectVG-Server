using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using ProjectVG.Application.Models.User;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private const int UID_LENGTH = 12;
        private const string UID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, username);
            }

            return new UserDto(user);
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, email);
            }

            return new UserDto(user);
        }

        public async Task<UserDto> CreateUserAsync(UserDto userDto)
        {
            await ValidateUserUniqueness(userDto);

            var user = userDto.ToEntity();
            
            // UID가 비어있으면 자동 생성
            if (string.IsNullOrEmpty(user.UID))
            {
                user.UID = await GenerateUniqueUIDAsync();
            }

            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("사용자 생성 완료: ID {UserId}, UID {UID}, 사용자명 {Username}", createdUser.Id, createdUser.UID, createdUser.Username);

            return new UserDto(createdUser);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user != null;
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null;
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UserDto userDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, userId);
            }

            user.Username = userDto.Username;
            user.Email = userDto.Email;
            user.Status = userDto.Status;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return new UserDto(updatedUser);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, userId);
            }

            // 실제 삭제 대신 상태를 Deleted로 변경
            user.Status = AccountStatus.Deleted;
            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("사용자 삭제 완료: ID {UserId}, 사용자명 {Username}", userId, user.Username);
            return true;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? new UserDto(user) : null;
        }

        public async Task<UserDto?> GetUserByUIDAsync(string uid)
        {
            var user = await _userRepository.GetByUIDAsync(uid);
            return user != null ? new UserDto(user) : null;
        }

        public async Task<bool> ExistsByUIDAsync(string uid)
        {
            var user = await _userRepository.GetByUIDAsync(uid);
            return user != null;
        }



        private async Task ValidateUserUniqueness(UserDto userDto)
        {
            if (await EmailExistsAsync(userDto.Email)) {
                throw new ValidationException(ErrorCode.EMAIL_ALREADY_EXISTS, userDto.Email);
            }

            if (await UsernameExistsAsync(userDto.Username)) {
                throw new ValidationException(ErrorCode.USERNAME_ALREADY_EXISTS, userDto.Username);
            }
        }

        /// <summary>
        /// 고유한 UID 생성
        /// </summary>
        private async Task<string> GenerateUniqueUIDAsync()
        {
            string uid;
            int attempts = 0;
            const int maxAttempts = 10;

            do
            {
                uid = GenerateRandomUID();
                attempts++;

                if (attempts > maxAttempts)
                {
                    throw new InvalidOperationException("UID 생성 시도 횟수 초과");
                }

            } while (await ExistsByUIDAsync(uid));

            return uid;
        }

        /// <summary>
        /// 랜덤 UID 생성
        /// </summary>
        private string GenerateRandomUID()
        {
            var randomBytes = new byte[UID_LENGTH];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var uid = new char[UID_LENGTH];
            for (int i = 0; i < UID_LENGTH; i++)
            {
                uid[i] = UID_CHARS[randomBytes[i] % UID_CHARS.Length];
            }

            return new string(uid);
        }
    }
}
