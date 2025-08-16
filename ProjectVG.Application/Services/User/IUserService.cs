using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// 사용자명으로 사용자 조회
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <summary>
/// 지정한 사용자 이름으로 사용자를 비동기 조회합니다.
/// </summary>
/// <param name="username">조회할 사용자의 고유 사용자 이름(로그인 ID).</param>
/// <returns>해당 사용자에 대한 UserDto를 비동기적으로 반환합니다.</returns>
        Task<UserDto> GetUserByUsernameAsync(string username);

        /// <summary>
        /// 이메일로 사용자 조회
        /// </summary>
        /// <param name="email">이메일</param>
        /// <summary>
/// 지정한 이메일 주소로 사용자를 비동기 조회합니다.
/// </summary>
/// <param name="email">조회할 사용자의 이메일 주소.</param>
/// <returns>조회된 사용자 정보를 담은 <see cref="UserDto"/>.</returns>
        Task<UserDto> GetUserByEmailAsync(string email);

        /// <summary>
        /// 사용자 생성
        /// </summary>
        /// <param name="userDto">사용자 정보</param>
        /// <summary>
/// 새 사용자를 비동기적으로 생성하고 생성된 사용자 정보를 반환합니다.
/// </summary>
/// <param name="userDto">생성할 사용자 정보(예: 이메일, 사용자명, 기타 속성)를 담은 DTO.</param>
/// <returns>생성된 사용자의 정보를 담은 <see cref="UserDto"/>.</returns>
        Task<UserDto> CreateUserAsync(UserDto userDto);

        /// <summary>
        /// 이메일 중복 확인
        /// </summary>
        /// <param name="email">이메일</param>
        /// <summary>
/// 지정된 이메일이 이미 등록되어 있는지 비동기로 확인합니다.
/// </summary>
/// <param name="email">확인할 이메일 주소.</param>
/// <returns>이메일이 이미 존재하면 true, 아니면 false를 반환하는 Task.</returns>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>
        /// 사용자명 중복 확인
        /// </summary>
        /// <param name="username">사용자명</param>
        /// <summary>
/// 지정된 사용자명이 이미 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="username">확인할 사용자명</param>
/// <returns>사용자명이 이미 존재하면 true, 아니면 false</returns>
        Task<bool> UsernameExistsAsync(string username);

        /// <summary>
        /// 사용자 존재 확인
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <summary>
/// 지정한 사용자 ID를 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="userId">확인할 사용자 식별자(Guid).</param>
/// <returns>사용자가 존재하면 true, 없으면 false를 반환합니다.</returns>
        Task<bool> UserExistsAsync(Guid userId);

        /// <summary>
        /// 사용자 정보 업데이트
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <summary>
/// 지정된 사용자 ID의 정보를 주어진 내용으로 비동기적으로 갱신하고 갱신된 사용자 정보를 반환합니다.
/// </summary>
/// <param name="userId">갱신할 사용자의 고유 식별자(Guid).</param>
/// <param name="userDto">적용할 사용자 정보(변경될 필드 포함).</param>
/// <returns>갱신이 반영된 UserDto를 담은 비동기 작업(Task).</returns>
        Task<UserDto> UpdateUserAsync(Guid userId, UserDto userDto);

        /// <summary>
        /// 사용자 삭제
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <summary>
/// 지정한 사용자 ID의 사용자를 비동기적으로 삭제합니다.
/// </summary>
/// <param name="userId">삭제할 사용자의 고유 식별자(Guid).</param>
/// <returns>삭제에 성공하면 <c>true</c>, 그렇지 않으면 <c>false</c>.</returns>
        Task<bool> DeleteUserAsync(Guid userId);
    }
} 