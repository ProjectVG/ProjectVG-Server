using System.Collections.Generic;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public interface IConnectionRegistry
	{
		/// <summary>
		/// 연결을 등록합니다
		/// </summary>
		void Register(string sessionId, IClientConnection connection);

		/// <summary>
		/// 연결을 해제합니다
		/// </summary>
		void Unregister(string sessionId);

		/// <summary>
		/// 연결을 조회합니다
		/// </summary>
		bool TryGet(string sessionId, out IClientConnection? connection);

		/// <summary>
		/// 연결 상태를 확인합니다
		/// </summary>
		bool IsConnected(string sessionId);

		/// <summary>
		/// 사용자 ID로 세션 ID 목록을 조회합니다
		/// </summary>
		IEnumerable<string> GetSessionIdsByUserId(string userId);

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// </summary>
		int GetActiveConnectionCount();
	}
}


