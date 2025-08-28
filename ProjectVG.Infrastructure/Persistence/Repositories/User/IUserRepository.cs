using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Users
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        /// <summary>
/// 지정한 사용자 이름에 해당하는 사용자를 비동기적으로 조회합니다.
/// </summary>
/// <param name="username">조회할 사용자의 사용자 이름(대소문자 처리 규칙은 구현체에 따름).</param>
/// <returns>조회된 User 객체를 담은 Task. 사용자가 존재하지 않으면 null을 반환할 수 있습니다.</returns>
Task<User?> GetByUsernameAsync(string username);
        /// <summary>
/// 지정한 이메일과 일치하는 사용자 엔터티를 비동기적으로 조회합니다.
/// </summary>
/// <param name="email">조회할 사용자의 이메일(정규화/대소문자 처리 규칙은 저장소 구현에 따름).</param>
/// <returns>일치하는 User를 포함한 Task; 사용자가 없으면 null을 반환합니다.</returns>
Task<User?> GetByEmailAsync(string email);
        /// <summary>
/// 지정된 외부 인증 공급자 식별자(providerId)에 해당하는 사용자를 비동기적으로 조회합니다.
/// </summary>
/// <param name="providerId">외부 인증 공급자에서 발급된 사용자 식별자(예: OAuth 제공자 ID).</param>
/// <returns>조회된 User 객체를 포함한 Task; 사용자가 없으면 null을 반환합니다.</returns>
Task<User?> GetByProviderIdAsync(string providerId);
        /// <summary>
/// 지정된 UID로 사용자를 비동기 조회합니다.
/// </summary>
/// <param name="uid">조회할 사용자의 고유 식별자(UID).</param>
/// <returns>일치하는 사용자를 반환합니다. 없으면 null을 반환합니다.</returns>
Task<User?> GetByUIDAsync(string uid);
        /// <summary>
/// 새 사용자(User) 엔티티를 비동기적으로 생성하여 저장소에 추가합니다.
/// </summary>
/// <param name="user">생성할 사용자 엔티티(저장 후 식별자나 생성 시점 등 영속화된 필드가 채워질 수 있음).</param>
/// <returns>저장소에 생성되어 반환된 User 인스턴스(영속화된 필드가 반영됨).</returns>
Task<User> CreateAsync(User user);
        /// <summary>
/// 기존 사용자 정보를 비동기적으로 갱신하고 갱신된 사용자 엔터티를 반환합니다.
/// </summary>
/// <param name="user">갱신할 사용자 엔터티(식별자(Id)가 존재하는 상태여야 함).</param>
/// <returns>갱신이 완료된 User 엔터티.</returns>
Task<User> UpdateAsync(User user);
        /// <summary>
/// 지정한 Id를 가진 사용자 엔티티를 비동기적으로 삭제합니다.
/// </summary>
/// <param name="id">삭제할 사용자의 고유 식별자(Guid).</param>
Task DeleteAsync(Guid id);
    }
} 