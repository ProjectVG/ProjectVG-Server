using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.SessionStorage
{
	public interface ISessionStorage
	{
		/// <summary>
		/// 세션을 조회합니다
		/// </summary>
		Task<SessionInfo?> GetAsync(string sessionId);

		/// <summary>
		/// 모든 세션을 조회합니다
		/// </summary>
		Task<IEnumerable<SessionInfo>> GetAllAsync();

		/// <summary>
		/// 세션을 생성합니다
		/// </summary>
		Task<SessionInfo> CreateAsync(SessionInfo session);

		/// <summary>
		/// 세션을 수정합니다
		/// </summary>
		Task<SessionInfo> UpdateAsync(SessionInfo session);

		/// <summary>
		/// 세션을 삭제합니다
		/// </summary>
		Task DeleteAsync(string sessionId);

		/// <summary>
		/// 세션 존재 여부를 확인합니다
		/// </summary>
		Task<bool> ExistsAsync(string sessionId);

		/// <summary>
		/// 활성 세션 수를 조회합니다
		/// </summary>
		Task<int> GetActiveSessionCountAsync();

		/// <summary>
		/// 사용자 ID로 세션을 조회합니다
		/// </summary>
		Task<IEnumerable<SessionInfo>> GetSessionsByUserIdAsync(string? userId);
	}
}


