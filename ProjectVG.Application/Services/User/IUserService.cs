using ProjectVG.Application.Models.User;

namespace ProjectVG.Application.Services.Users
{
    public interface IUserService
    {
        /// <summary>
/// 지정된 생성 명령을 사용하여 새 사용자를 비동기적으로 생성하고 생성된 사용자의 정보를 반환합니다.
/// </summary>
/// <param name="command">생성에 필요한 사용자 정보(Username, Email, ProviderId, Provider)를 포함하는 명령 객체.</param>
/// <returns>생성된 사용자를 나타내는 UserDto를 포함하는 비동기 작업.</returns>
Task<UserDto> CreateUserAsync(UserCreateCommand command);
        /// <summary>
/// 지정된 ID의 사용자를 비동기적으로 삭제합니다.
/// </summary>
/// <param name="userId">삭제할 사용자의 고유 식별자(Guid).</param>
/// <returns>삭제에 성공하면 <c>true</c>, 사용자를 찾을 수 없거나 삭제에 실패하면 <c>false</c>를 반환하는 비동기 작업.</returns>
Task<bool> DeleteUserAsync(Guid userId);

        /// <summary>
/// 지정한 사용자 ID로 사용자를 비동기적으로 조회합니다.
/// </summary>
/// <param name="userId">조회할 사용자의 고유 식별자(Guid).</param>
/// <returns>해당 ID의 사용자 정보를 담은 <see cref="UserDto"/> 객체 또는 존재하지 않으면 null을 반환합니다.</returns>
Task<UserDto?> TryGetByIdAsync(Guid userId);
        /// <summary>
/// 지정한 외부 UID로 사용자를 비동기 조회합니다.
/// </summary>
/// <param name="uid">외부 제공자에서 발급된 사용자 식별자(UID).</param>
/// <returns>
/// 조회된 사용자의 UserDto 객체를 반환합니다. 해당 UID를 가진 사용자가 없으면 null을 반환합니다.
/// </returns>
Task<UserDto?> TryGetByUidAsync(string uid);
        /// <summary>
/// 주어진 사용자명(username)에 해당하는 사용자를 비동기적으로 조회합니다.
/// </summary>
/// <param name="username">조회할 사용자의 사용자명(대소문자 처리 정책은 호출자에 따라 달라질 수 있음).</param>
/// <returns>
/// 조회된 사용자의 UserDto 인스턴스 또는 존재하지 않으면 null을 반환합니다.
/// </returns>
Task<UserDto?> TryGetByUsernameAsync(string username);
        /// <summary>
— 지정된 외부 인증 제공자(provider)와 공급자 ID(providerId)에 연결된 사용자를 비동기적으로 조회합니다.
</summary>
/** <param name="provider">외부 인증 제공자 이름(예: "google", "github").</param>
    <param name="providerId">해당 제공자에서 발급된 사용자의 고유 식별자.</param>
    <returns>해당 제공자·ID에 매칭되는 <see cref="UserDto"/>를 반환합니다. 없으면 null을 반환합니다.</returns> */
Task<UserDto?> TryGetByProviderAsync(string provider, string providerId);

        /// <summary>
/// 지정한 사용자 ID를 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="userId">확인할 사용자의 고유 식별자(Guid).</param>
/// <returns>
/// 사용자가 존재하면 <c>true</c>, 존재하지 않으면 <c>false</c>를 반환하는 비동기 작업.
/// </returns>
Task<bool> ExistsByIdAsync(Guid userId);
        /// <summary>
/// 지정된 UID를 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="uid">확인할 사용자의 고유 식별자(UID).</param>
/// <returns>해당 UID를 가진 사용자가 존재하면 true, 그렇지 않으면 false를 반환하는 작업.</returns>
Task<bool> ExistsByUidAsync(string uid);
        /// <summary>
/// 주어진 이메일을 가진 사용자가 시스템에 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="email">확인할 이메일 주소.</param>
/// <returns>해당 이메일을 가진 사용자가 존재하면 true, 그렇지 않으면 false를 반환합니다.</returns>
Task<bool> ExistsByEmailAsync(string email);
        /// <summary>
/// 주어진 사용자명(username)을 가진 사용자가 존재하는지 비동기적으로 확인합니다.
/// </summary>
/// <param name="username">존재 여부를 확인할 대상 사용자명.</param>
/// <returns>사용자가 존재하면 true, 그렇지 않으면 false를 반환하는 Task.</returns>
Task<bool> ExistsByUsernameAsync(string username);
    }

    public record UserCreateCommand(
        string Username,
        string Email,
        string ProviderId,
        string Provider
    );
}