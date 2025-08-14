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
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("사용자명 {Username}인 사용자를 찾을 수 없습니다", username);
                    return null;
                }

                var userDto = new UserDto(user);
                _logger.LogDebug("사용자명 {Username}인 사용자를 조회했습니다", username);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username}인 사용자 조회 중 오류가 발생했습니다", username);
                throw;
            }
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("이메일 {Email}인 사용자를 찾을 수 없습니다", email);
                    return null;
                }

                var userDto = new UserDto(user);
                _logger.LogDebug("이메일 {Email}인 사용자를 조회했습니다", email);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email}인 사용자 조회 중 오류가 발생했습니다", email);
                throw;
            }
        }

        public async Task<UserDto> CreateUserAsync(UserDto userDto)
        {
            try
            {
                // 중복 검사
                if (await EmailExistsAsync(userDto.Email))
                {
                    throw new InvalidOperationException($"이메일 '{userDto.Email}'이 이미 존재합니다.");
                }

                if (await UsernameExistsAsync(userDto.Username))
                {
                    throw new InvalidOperationException($"사용자명 '{userDto.Username}'이 이미 존재합니다.");
                }

                var user = userDto.ToEntity();
                var createdUser = await _userRepository.CreateAsync(user);
                var createdUserDto = new UserDto(createdUser);
                
                _logger.LogInformation("사용자를 생성했습니다. ID: {UserId}, 사용자명: {Username}", 
                    createdUserDto.Id, createdUserDto.Username);
                return createdUserDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email}인 사용자 생성 중 오류가 발생했습니다", userDto.Email);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email} 존재 여부 확인 중 오류가 발생했습니다", email);
                throw;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username} 존재 여부 확인 중 오류가 발생했습니다", username);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 ID {UserId} 존재 여부 확인 중 오류가 발생했습니다", userId);
                return false;
            }
        }
    }
}
