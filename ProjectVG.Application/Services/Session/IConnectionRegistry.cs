using System.Collections.Generic;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public interface IConnectionRegistry
	{
		/// <summary>
		/// 연결을 등록합니다
		/// <summary>
/// 지정된 사용자 ID에 대해 클라이언트 연결을 등록한다.
/// </summary>
/// <param name="userId">연결을 등록할 사용자 식별자(빈 문자열이나 null은 허용되지 않아야 함).</param>
		void Register(string userId, IClientConnection connection);

		/// <summary>
		/// 연결을 해제합니다
		/// <summary>
/// 지정된 사용자 ID에 연결된 클라이언트 연결을 등록 해제(제거)합니다.
/// </summary>
/// <param name="userId">연결을 제거할 사용자 식별자.</param>
		void Unregister(string userId);

		/// <summary>
		/// 연결을 조회합니다
		/// <summary>
/// 지정된 사용자 ID에 연결된 클라이언트 연결을 조회하려 시도합니다.
/// </summary>
/// <param name="userId">조회할 사용자 식별자.</param>
/// <param name="connection">사용자에 연결이 존재하면 해당 연결을 출력합니다. 연결이 없으면 null이 설정됩니다.</param>
/// <returns>연결이 존재하면 true, 없으면 false.</returns>
		bool TryGet(string userId, out IClientConnection? connection);

		/// <summary>
		/// 연결 상태를 확인합니다
		/// <summary>
/// 지정된 사용자가 현재 활성 연결을 보유하고 있는지 확인합니다.
/// </summary>
/// <param name="userId">확인할 사용자의 고유 식별자.</param>
/// <returns>해당 사용자가 하나 이상의 활성 연결을 가지고 있으면 true, 그렇지 않으면 false.</returns>
		bool IsConnected(string userId);

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// </summary>
		int GetActiveConnectionCount();
	}
}


