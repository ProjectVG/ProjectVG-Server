using System.Collections.Generic;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public interface IConnectionRegistry
	{
		/// <summary>
		/// 연결을 등록합니다
		/// </summary>
		void Register(string userId, IClientConnection connection);

		/// <summary>
		/// 연결을 해제합니다
		/// </summary>
		void Unregister(string userId);

		/// <summary>
		/// 연결을 조회합니다
		/// </summary>
		bool TryGet(string userId, out IClientConnection? connection);

		/// <summary>
		/// 연결 상태를 확인합니다
		/// </summary>
		bool IsConnected(string userId);

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// </summary>
		int GetActiveConnectionCount();

		/// <summary>
		/// 모든 활성 연결을 반환합니다
		/// </summary>
		IEnumerable<KeyValuePair<string, IClientConnection>> GetAllActiveConnections();

		/// <summary>
		/// 모든 활성 사용자 ID를 반환합니다
		/// </summary>
		IEnumerable<string> GetAllActiveUserIds();
	}
}


