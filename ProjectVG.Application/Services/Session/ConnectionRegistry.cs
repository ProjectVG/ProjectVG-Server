using System.Collections.Concurrent;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public class ConnectionRegistry : IConnectionRegistry
	{
		private readonly ILogger<ConnectionRegistry> _logger;
		private readonly ConcurrentDictionary<string, IClientConnection> _connections = new();

		/// <summary>
		/// ConnectionRegistry의 새 인스턴스를 초기화합니다.
		/// </summary>
		/// <remarks>
		/// 내부의 스레드 안전한 연결 저장소를 초기화하고 로깅을 위한 인스턴스를 보관합니다.
		/// </remarks>
		public ConnectionRegistry(ILogger<ConnectionRegistry> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 연결을 등록합니다
		/// <summary>
		/// 지정된 사용자 ID에 대해 클라이언트 연결을 등록하거나 기존 연결을 교체합니다.
		/// </summary>
		/// <param name="userId">연결을 식별하는 사용자 고유 ID(키). 기존 항목이 있으면 새 연결로 대체됩니다.</param>
		/// <remarks>
		/// 연결은 내부의 스레드 안전한 저장소(ConcurrentDictionary)에 저장됩니다.
		/// </remarks>
		public void Register(string userId, IClientConnection connection)
		{
			_connections[userId] = connection;
			_logger.LogDebug("연결 등록: {UserId}", userId);
		}

		/// <summary>
		/// 연결을 해제합니다
		/// <summary>
		/// 지정한 사용자 ID에 연결된 클라이언트 연결을 레지스트리에서 제거합니다.
		/// </summary>
		/// <param name="userId">제거할 연결이 등록된 사용자 식별자.</param>
		/// <remarks>
		/// 등록된 연결이 없으면 예외를 발생시키지 않고 조용히 무시합니다.
		/// </remarks>
		public void Unregister(string userId)
		{
			if (_connections.TryRemove(userId, out var removed))
			{
				_logger.LogDebug("연결 해제: {UserId}", userId);
			}
			else
			{
				_logger.LogWarning("해제 대상 세션을 찾을 수 없음: {UserId}", userId);
			}
		}

		/// <summary>
		/// 연결을 조회합니다
		/// <summary>
		/// 지정된 사용자 ID에 연관된 클라이언트 연결을 시도해 가져옵니다.
		/// </summary>
		/// <param name="userId">조회할 사용자 식별자.</param>
		/// <param name="connection">찾은 경우 해당 사용자에 대한 IClientConnection 인스턴스(없는 경우 null)를 설정하는出力 매개변수.</param>
		/// <returns>해당 사용자 ID의 연결이 존재하면 true, 그렇지 않으면 false.</returns>
		public bool TryGet(string userId, out IClientConnection? connection)
		{
			var ok = _connections.TryGetValue(userId, out var conn);
			connection = conn;
			return ok;
		}

		/// <summary>
		/// 연결 상태를 확인합니다
		/// <summary>
		/// 주어진 세션 ID에 해당하는 연결이 등록되어 있는지 확인합니다.
		/// </summary>
		/// <param name="sessionId">확인할 세션 ID.</param>
		/// <returns>해당 세션 ID에 연결이 존재하면 <c>true</c>, 그렇지 않으면 <c>false</c>.</returns>
		public bool IsConnected(string sessionId)
		{
			return _connections.ContainsKey(sessionId);
		}

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// <summary>
		/// 현재 레지스트리에 저장된 활성 연결의 수를 반환합니다.
		/// </summary>
		/// <returns>현재 활성 연결(저장된 항목)의 총 개수.</returns>
		public int GetActiveConnectionCount()
		{
			return _connections.Count;
		}
	}
}


