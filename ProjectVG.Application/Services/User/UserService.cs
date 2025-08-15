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
            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("사용자 생성 완료: ID {UserId}, 사용자명 {Username}", createdUser.Id, createdUser.Username);

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

            user.Name = userDto.Name;
            user.Username = userDto.Username;
            user.Email = userDto.Email;
            user.IsActive = userDto.IsActive;

            var updatedUser = await _userRepository.UpdateAsync(user);
            return new UserDto(updatedUser);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, userId);
            }

            await _userRepository.DeleteAsync(userId);
            _logger.LogInformation("사용자 삭제 완료: ID {UserId}, 사용자명 {Username}", userId, user.Username);
            return true;
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
    }
}
