using System.Collections.Concurrent;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public class ConnectionRegistry : IConnectionRegistry
	{
		private readonly ILogger<ConnectionRegistry> _logger;
		private readonly ConcurrentDictionary<string, IClientConnection> _connections = new();
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userIdToSessionIds = new();

		/// <summary>
		/// ConnectionRegistry 인스턴스를 초기화합니다.
		/// </summary>
		public ConnectionRegistry(ILogger<ConnectionRegistry> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 연결을 등록합니다
		/// <summary>
		/// 지정된 세션 ID로 클라이언트 연결을 등록하고, 연결에 사용자 ID가 있으면 해당 사용자에 대한 세션 매핑을 업데이트합니다.
		/// </summary>
		/// <param name="sessionId">등록할 세션의 고유 ID.</param>
		/// <param name="connection">등록할 클라이언트 연결 객체 (IClientConnection). UserId가 설정된 경우 사용자→세션 매핑에 추가됩니다.</param>
		public void Register(string sessionId, IClientConnection connection)
		{
			_connections[sessionId] = connection;

			if (!string.IsNullOrEmpty(connection.UserId))
			{
				var set = _userIdToSessionIds.GetOrAdd(connection.UserId!, _ => new ConcurrentDictionary<string, byte>());
				set[sessionId] = 1;
			}

			_logger.LogDebug("연결 등록: {SessionId}, 사용자: {UserId}", sessionId, connection.UserId);
		}

		/// <summary>
		/// 연결을 해제합니다
		/// <summary>
		/// 지정한 세션 ID에 해당하는 클라이언트 연결을 레지스트리에서 제거하고, 해당 연결이 연관된 사용자 매핑에서 세션을 삭제합니다.
		/// </summary>
		/// <param name="sessionId">제거할 연결의 세션 식별자.</param>
		public void Unregister(string sessionId)
		{
			if (_connections.TryRemove(sessionId, out var removed))
			{
				RemoveFromUserMapping(removed.UserId, sessionId);
				_logger.LogDebug("연결 해제: {SessionId}", sessionId);
			}
			else
			{
				_logger.LogWarning("해제 대상 세션을 찾을 수 없음: {SessionId}", sessionId);
			}
		}

		/// <summary>
		/// 연결을 조회합니다
		/// <summary>
		/// 지정된 세션 ID에 대한 활성 클라이언트 연결을 시도하여 검색합니다.
		/// </summary>
		/// <param name="sessionId">조회할 세션의 ID.</param>
		/// <param name="connection">찾은 연결을 할당하는 출력 파라미터. 없으면 null이 할당됩니다.</param>
		/// <returns>세션 ID에 해당하는 연결을 찾으면 true, 그렇지 않으면 false.</returns>
		public bool TryGet(string sessionId, out IClientConnection? connection)
		{
			var ok = _connections.TryGetValue(sessionId, out var conn);
			connection = conn;
			return ok;
		}

		/// <summary>
		/// 연결 상태를 확인합니다
		/// <summary>
		/// 지정한 세션 ID를 가진 활성 연결이 레지스트리에 존재하는지 확인합니다.
		/// </summary>
		/// <param name="sessionId">확인할 연결의 세션 식별자.</param>
		/// <returns>해당 세션 ID에 대한 활성 연결이 있으면 true, 그렇지 않으면 false.</returns>
		public bool IsConnected(string sessionId)
		{
			return _connections.ContainsKey(sessionId);
		}

		/// <summary>
		/// 사용자 ID로 세션 ID 목록을 조회합니다
		/// <summary>
		— 지정된 사용자 ID에 매핑된 활성 세션 ID들을 반환합니다.
		/// </summary>
		/// <param name="userId">조회할 사용자 식별자.</param>
		/// <returns>해당 사용자에 연결된 세션 ID들의 열거형. 사용자가 없거나 세션이 없으면 빈 열거형을 반환합니다.</returns>
		public IEnumerable<string> GetSessionIdsByUserId(string userId)
		{
			if (_userIdToSessionIds.TryGetValue(userId, out var set))
			{
				return set.Keys;
			}
			return Enumerable.Empty<string>();
		}

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// <summary>
		/// 현재 레지스트리에 등록된 활성 연결(세션)의 총 개수를 반환합니다.
		/// </summary>
		/// <returns>활성 연결의 수.</returns>
		public int GetActiveConnectionCount()
		{
			return _connections.Count;
		}

		/// <summary>
		/// 사용자 ID에 연결된 세션 매핑에서 지정된 세션 ID를 제거합니다.
		/// </summary>
		/// <remarks>
		/// userId가 null이거나 빈 문자열이면 아무 작업도 수행하지 않습니다.
		/// 지정된 세션 ID를 제거한 후 해당 사용자의 세션 집합이 비어 있으면 사용자 항목 전체를 사전에서 제거합니다.
		/// </remarks>
		/// <param name="userId">세션을 제거할 대상 사용자의 ID(널 또는 빈 문자열일 수 있음).</param>
		/// <param name="sessionId">제거할 세션의 ID.</param>
		private void RemoveFromUserMapping(string? userId, string sessionId)
		{
			if (!string.IsNullOrEmpty(userId) && _userIdToSessionIds.TryGetValue(userId!, out var set))
			{
				set.TryRemove(sessionId, out _);
				if (set.IsEmpty)
				{
					_userIdToSessionIds.TryRemove(userId!, out _);
				}
			}
		}

		/// <summary>
		/// 세션 ID에 해당하는 클라이언트 연결을 시도해 조회합니다.
		/// </summary>
		/// <param name="sessionId">조회할 세션의 식별자.</param>
		/// <param name="connection">찾은 연결을 출력합니다. 존재하지 않으면 null이 할당됩니다.</param>
		/// <returns>연결을 찾으면 true, 찾지 못하면 false.</returns>
		/// <remarks>세션을 찾지 못할 경우 경고 로그를 남깁니다.</remarks>
		private bool TryGetConnection(string sessionId, out IClientConnection? connection)
		{
			if (_connections.TryGetValue(sessionId, out connection))
			{
				return true;
			}

			_logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
			return false;
		}
	}
}


