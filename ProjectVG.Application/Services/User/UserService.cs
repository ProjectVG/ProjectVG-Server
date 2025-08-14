using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using ProjectVG.Application.Models.User;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.User
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

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("사용자명 {Username}인 사용자를 찾을 수 없습니다", username);
                return null;
            }

            return new UserDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("이메일 {Email}인 사용자를 찾을 수 없습니다", email);
                return null;
            }

            return new UserDto(user);
        }

        public async Task<UserDto> CreateUserAsync(UserDto userDto)
        {
            await ValidateUserUniqueness(userDto);

            var user = userDto.ToEntity();
            var createdUser = await _userRepository.CreateAsync(user);
            
            _logger.LogInformation("사용자 생성 완료: ID {UserId}, 사용자명 {Username}", 
                createdUser.Id, createdUser.Username);
            
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

        private async Task ValidateUserUniqueness(UserDto userDto)
        {
            if (await EmailExistsAsync(userDto.Email))
            {
                throw new InvalidOperationException($"이메일 '{userDto.Email}'이 이미 존재합니다.");
            }

            if (await UsernameExistsAsync(userDto.Username))
            {
                throw new InvalidOperationException($"사용자명 '{userDto.Username}'이 이미 존재합니다.");
            }
        }
    }
}
