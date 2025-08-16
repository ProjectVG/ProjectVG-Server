using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Persistence.Session
{
	public interface ISessionStorage
	{
		/// <summary>
		/// 세션을 조회합니다
		/// <summary>
/// 지정한 세션 ID에 해당하는 세션 정보를 비동기로 조회합니다.
/// </summary>
/// <param name="sessionId">조회할 세션의 고유 식별자.</param>
/// <returns>해당 ID의 SessionInfo를 포함한 Task. 세션이 존재하지 않으면 null을 반환합니다.</returns>
		Task<SessionInfo?> GetAsync(string sessionId);

		/// <summary>
		/// 모든 세션을 조회합니다
		/// <summary>
/// 저장된 모든 세션 정보를 비동기적으로 조회합니다.
/// </summary>
/// <returns>
/// 조회된 SessionInfo 객체들의 컬렉션을 담은 Task. 세션이 없으면 빈 컬렉션을 반환할 수 있습니다.
/// </returns>
		Task<IEnumerable<SessionInfo>> GetAllAsync();

		/// <summary>
		/// 세션을 생성합니다
		/// <summary>
/// 새 세션을 영구 저장소에 생성하고 생성된 세션 정보를 반환합니다.
/// </summary>
/// <param name="session">저장할 세션 정보 객체.</param>
/// <returns>저장 후 반환된 세션 정보(저장소에서 할당된 식별자나 필드가 포함될 수 있음).</returns>
		Task<SessionInfo> CreateAsync(SessionInfo session);

		/// <summary>
		/// 세션을 수정합니다
		/// <summary>
/// 식별자에 해당하는 기존 세션을 비동기적으로 갱신하고 갱신된 세션 정보를 반환합니다.
/// </summary>
/// <param name="session">갱신할 세션 정보(식별자 포함).</param>
/// <returns>갱신된 SessionInfo를 반환하는 Task.</returns>
		Task<SessionInfo> UpdateAsync(SessionInfo session);

		/// <summary>
		/// 세션을 삭제합니다
		/// <summary>
/// 주어진 세션 ID에 해당하는 세션을 비동기적으로 삭제합니다.
/// </summary>
/// <param name="sessionId">삭제할 세션의 식별자.</param>
		Task DeleteAsync(string sessionId);

		/// <summary>
		/// 세션 존재 여부를 확인합니다
		/// <summary>
/// 주어진 세션 ID가 저장소에 존재하는지 비동기로 확인합니다.
/// </summary>
/// <param name="sessionId">검사할 세션의 식별자.</param>
/// <returns>세션이 존재하면 true, 존재하지 않으면 false를 반환하는 비동기 작업.</returns>
		Task<bool> ExistsAsync(string sessionId);

		/// <summary>
		/// 활성 세션 수를 조회합니다
		/// <summary>
/// 현재 활성 상태인 세션의 개수를 비동기적으로 반환합니다.
/// </summary>
/// <returns>활성 세션의 총 개수를 나타내는 <see cref="Task{Int32}"/>.</returns>
		Task<int> GetActiveSessionCountAsync();

		/// <summary>
		/// 사용자 ID로 세션을 조회합니다
		/// <summary>
/// 지정된 사용자 ID와 연관된 세션 목록을 비동기적으로 반환합니다.
/// </summary>
/// <param name="userId">조회할 사용자 ID. null이면 사용자와 연관되지 않은(익명) 세션을 조회합니다.</param>
/// <returns>해당 사용자 ID와 연관된 SessionInfo 컬렉션(없으면 빈 컬렉션).</returns>
		Task<IEnumerable<SessionInfo>> GetSessionsByUserIdAsync(string? userId);
	}
}


