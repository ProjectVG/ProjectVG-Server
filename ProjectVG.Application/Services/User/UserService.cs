using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using ProjectVG.Application.Models.User;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// UserService의 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 주입된 의존성(사용자 저장소 및 로거)을 내부 필드에 저장하여 서비스 메서드에서 사용합니다.
        /// </remarks>
        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// 지정한 사용자 이름으로 사용자를 조회하여 UserDto를 반환합니다.
        /// </summary>
        /// <param name="username">조회할 사용자의 고유한 사용자 이름(Username).</param>
        /// <returns>조회된 사용자의 정보를 담은 <see cref="UserDto"/> 객체.</returns>
        /// <exception cref="NotFoundException">해당 사용자 이름을 가진 사용자가 존재하지 않을 경우 발생합니다.</exception>
        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, username);
            }

            return new UserDto(user);
        }

        /// <summary>
        /// 지정된 이메일에 해당하는 사용자를 조회하여 UserDto로 반환합니다.
        /// </summary>
        /// <param name="email">조회할 사용자의 이메일 주소.</param>
        /// <returns>해당 이메일을 가진 사용자의 정보를 담은 <see cref="UserDto"/>.</returns>
        /// <exception cref="NotFoundException">해당 이메일을 가진 사용자가 존재하지 않을 경우, ErrorCode.USER_NOT_FOUND와 함께 발생합니다.</exception>
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) {
                throw new NotFoundException(ErrorCode.USER_NOT_FOUND, email);
            }

            return new UserDto(user);
        }

        /// <summary>
        /// 새 사용자를 생성합니다.
        /// </summary>
        /// <param name="userDto">생성할 사용자 정보를 담은 DTO(이메일, 사용자명 등).</param>
        /// <returns>생성된 사용자 정보를 담은 <see cref="UserDto"/> 인스턴스.</returns>
        /// <exception cref="ValidationException">
        /// 이메일 또는 사용자명이 이미 존재할 경우 발생합니다.
        /// ErrorCode: EMAIL_ALREADY_EXISTS 또는 USERNAME_ALREADY_EXISTS와 함께 던져집니다.
        /// </exception>
        public async Task<UserDto> CreateUserAsync(UserDto userDto)
        {
            await ValidateUserUniqueness(userDto);

            var user = userDto.ToEntity();
            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("사용자 생성 완료: ID {UserId}, 사용자명 {Username}", createdUser.Id, createdUser.Username);

            return new UserDto(createdUser);
        }

        /// <summary>
        /// 지정한 이메일을 가진 사용자가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="email">확인할 이메일 주소.</param>
        /// <returns>해당 이메일을 가진 사용자가 존재하면 <c>true</c>, 없으면 <c>false</c>를 반환합니다.</returns>
        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        }

        /// <summary>
        /// 지정된 사용자 이름을 가진 사용자가 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <param name="username">확인할 사용자 이름.</param>
        /// <returns>사용자가 존재하면 <c>true</c>, 그렇지 않으면 <c>false</c>.</returns>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user != null;
        }

        /// <summary>
        /// 지정한 사용자 ID를 가진 사용자가 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <param name="userId">확인할 사용자의 고유 식별자(Guid).</param>
        /// <returns>사용자가 존재하면 true, 없으면 false를 반환하는 Task.</returns>
        public async Task<bool> UserExistsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null;
        }

        /// <summary>
        /// 지정한 사용자 ID의 사용자 정보를 주어진 DTO 값으로 업데이트하고 업데이트된 UserDto를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 존재하지 않는 사용자 ID가 주어지면 <see cref="NotFoundException"/>을 던집니다.
        /// </remarks>
        /// <param name="userId">업데이트 대상 사용자의 식별자(Guid).</param>
        /// <param name="userDto">새로 적용할 사용자 데이터(이름, 사용자이름, 이메일, 활성 여부를 사용).</param>
        /// <returns>업데이트 후 저장된 사용자 정보를 담은 <see cref="UserDto"/>.</returns>
        /// <exception cref="NotFoundException">지정한 userId에 해당하는 사용자가 존재하지 않을 경우.</exception>
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

        /// <summary>
        /// 지정한 ID의 사용자를 삭제합니다.
        /// </summary>
        /// <param name="userId">삭제할 사용자의 식별자(Guid).</param>
        /// <returns>삭제가 성공하면 true를 반환합니다.</returns>
        /// <exception cref="NotFoundException">지정한 ID의 사용자가 존재하지 않을 경우 발생합니다. (ErrorCode: USER_NOT_FOUND)</exception>
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

        /// <summary>
        /// 주어진 사용자 정보의 이메일과 사용자명(Username)이 고유한지 확인합니다.
        /// </summary>
        /// <param name="userDto">검증할 사용자 데이터 전달 객체(이메일 및 사용자명 포함).</param>
        /// <exception cref="ValidationException">
        /// 이메일이 이미 존재하는 경우에는 ErrorCode.EMAIL_ALREADY_EXISTS와 해당 이메일을 담아 발생합니다.
        /// 사용자명이 이미 존재하는 경우에는 ErrorCode.USERNAME_ALREADY_EXISTS와 해당 사용자명을 담아 발생합니다.
        /// </exception>
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
