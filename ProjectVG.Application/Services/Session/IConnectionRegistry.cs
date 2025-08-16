using System.Collections.Generic;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public interface IConnectionRegistry
	{
		/// <summary>
		/// 연결을 등록합니다
		/// <summary>
/// 지정된 세션 ID에 클라이언트 연결을 등록합니다.
/// </summary>
/// <param name="sessionId">등록할 연결에 대응되는 세션 식별자.</param>
		void Register(string sessionId, IClientConnection connection);

		/// <summary>
		/// 연결을 해제합니다
		/// <summary>
/// 지정된 세션 ID에 연결된 클라이언트 연결을 레지스트리에서 제거한다.
/// 존재하지 않는 세션 ID인 경우 아무 작업도 수행하지 않는다.
/// </summary>
/// <param name="sessionId">제거하려는 연결의 세션 고유 식별자.</param>
		void Unregister(string sessionId);

		/// <summary>
		/// 연결을 조회합니다
		/// <summary>
/// 주어진 세션 ID에 대해 등록된 클라이언트 연결을 시도하여 가져옵니다.
/// </summary>
/// <param name="sessionId">조회할 세션의 식별자.</param>
/// <param name="connection">찾은 경우 해당 세션에 연결된 IClientConnection 인스턴스(없으면 null)를 출력합니다.</param>
/// <returns>연결이 존재하면 true; 존재하지 않으면 false.</returns>
		bool TryGet(string sessionId, out IClientConnection? connection);

		/// <summary>
		/// 연결 상태를 확인합니다
		/// <summary>
/// 지정된 세션 ID에 대해 활성 연결이 존재하는지 확인합니다.
/// </summary>
/// <param name="sessionId">확인할 세션의 식별자.</param>
/// <returns>세션에 대한 활성 연결이 있으면 <c>true</c>, 없으면 <c>false</c>를 반환합니다.</returns>
		bool IsConnected(string sessionId);

		/// <summary>
		/// 사용자 ID로 세션 ID 목록을 조회합니다
		/// <summary>
/// 지정된 사용자 ID와 연관된 세션 ID들을 반환합니다.
/// </summary>
/// <param name="userId">조회할 사용자 식별자.</param>
/// <returns>해당 사용자에 속한 세션 ID들의 열거형(세션이 없으면 빈 열거형).</returns>
		IEnumerable<string> GetSessionIdsByUserId(string userId);

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// <summary>
/// 현재 레지스트리에 등록되어 있는 활성 연결(세션)의 개수를 반환합니다.
/// </summary>
/// <returns>활성 연결 수를 나타내는 정수.</returns>
		int GetActiveConnectionCount();
	}
}


