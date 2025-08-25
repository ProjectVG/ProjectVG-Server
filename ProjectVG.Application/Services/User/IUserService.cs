using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// 사용자명으로 사용자 조회
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <returns>사용자 정보</returns>
        Task<UserDto> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 이메일로 사용자 조회
        /// </summary>
        /// <param name="email">이메일</param>
        /// <returns>사용자 정보</returns>
        Task<UserDto> GetUserByEmailAsync(string email);

        /// <summary>
        /// 사용자 생성
        /// </summary>
        /// <param name="userDto">사용자 정보</param>
        /// <returns>생성된 사용자 정보</returns>
        Task<UserDto> CreateUserAsync(UserDto userDto);

        /// <summary>
        /// 이메일 중복 확인
        /// </summary>
        /// <param name="email">이메일</param>
        /// <returns>이메일 중복 여부</returns>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// 사용자명 중복 확인
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <returns>사용자명 중복 여부</returns>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// 사용자 존재 확인
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>사용자 존재 여부</returns>
        Task<bool> UserExistsAsync(Guid userId);

        /// <summary>
        /// 사용자 정보 업데이트
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="userDto">사용자 정보</param>
        Task<UserDto> UpdateUserAsync(Guid userId, UserDto userDto);

        /// <summary>
        /// 사용자 삭제
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>삭제 여부</returns>
        Task<bool> DeleteUserAsync(Guid userId);

        /// <summary>
        /// 사용자 ID로 사용자 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>사용자 정보</returns>
        Task<UserDto?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// UID로 사용자 조회
        /// </summary>
        /// <param name="uid">외부 노출용 UID</param>
        /// <returns>사용자 정보</returns>
        Task<UserDto?> GetUserByUIDAsync(string uid);

        /// <summary>
        /// UID 중복 확인
        /// </summary>
        /// <param name="uid">외부 노출용 UID</param>
        /// <returns>UID 중복 여부</returns>
        Task<bool> ExistsByUIDAsync(string uid);
    }
} 