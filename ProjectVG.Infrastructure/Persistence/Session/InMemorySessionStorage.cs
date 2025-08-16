using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Persistence.Session
{
	public class InMemorySessionStorage : ISessionStorage
	{
		private readonly Dictionary<string, SessionInfo> _sessions = new();
		private readonly ILogger<InMemorySessionStorage> _logger;
		private readonly object _lock = new();

		/// <summary>
		/// InMemory 기반의 세션 저장소 인스턴스를 초기화합니다.
		/// </summary>
		public InMemorySessionStorage(ILogger<InMemorySessionStorage> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 지정된 세션 ID로 저장소에서 세션을 조회하여 반환합니다.
		/// 스레드 안전하게 동작하며, 해당 ID의 세션이 없으면 null을 반환합니다.
		/// </summary>
		/// <param name="sessionId">조회할 세션의 고유 식별자.</param>
		/// <returns>조회된 <see cref="SessionInfo"/> 인스턴스를 래핑한 <see cref="Task"/>; 없으면 null을 래핑하여 반환합니다.</returns>
		public Task<SessionInfo?> GetAsync(string sessionId)
		{
			lock (_lock)
			{
				_sessions.TryGetValue(sessionId, out var session);
				return Task.FromResult(session);
			}
		}

		/// <summary>
		/// 저장소에 현재 보관된 모든 세션을 반환합니다.
		/// </summary>
		/// <returns>
		/// 현재 저장된 세션들의 열거(Task&lt;IEnumerable&lt;SessionInfo&gt;&gt;). 호출 시 내부 동기화를 사용하여 접근합니다.
		/// </returns>
		public Task<IEnumerable<SessionInfo>> GetAllAsync()
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.Values.AsEnumerable());
			}
		}

		/// <summary>
		/// 지정한 세션 정보를 인메모리 저장소에 저장합니다.
		/// 기존에 동일한 SessionId가 있으면 해당 항목을 덮어쓰며, 새로 추가된 경우 디버그 로그를 남깁니다.
		/// 이 메서드는 내부 락으로 보호되어 스레드 안전하게 동작합니다.
		/// </summary>
		/// <param name="session">저장할 세션 정보. SessionId 값을 키로 사용합니다.</param>
		/// <returns>저장된 동일한 <see cref="SessionInfo"/> 인스턴스를 감싼 <see cref="Task"/>.</returns>
		public Task<SessionInfo> CreateAsync(SessionInfo session)
		{
			lock (_lock)
			{
				if (_sessions.ContainsKey(session.SessionId))
				{
					_sessions[session.SessionId] = session;
					return Task.FromResult(session);
				}

				_sessions[session.SessionId] = session;
				_logger.LogDebug("세션을 생성했습니다: {SessionId}", session.SessionId);
				return Task.FromResult(session);
			}
		}

		/// <summary>
		/// 주어진 세션 정보로 기존 세션을 갱신하고 갱신된 세션을 반환합니다.
		/// </summary>
		/// <param name="session">갱신할 세션 정보(유효한 SessionId를 포함해야 함).</param>
		/// <returns>갱신된 <see cref="SessionInfo"/>를 담은 <see cref="Task"/>.</returns>
		/// <exception cref="KeyNotFoundException">지정한 SessionId가 저장소에 존재하지 않을 경우 발생합니다.</exception>
		public Task<SessionInfo> UpdateAsync(SessionInfo session)
		{
			lock (_lock)
			{
				if (!_sessions.ContainsKey(session.SessionId))
				{
					throw new KeyNotFoundException($"세션 ID {session.SessionId}를 찾을 수 없습니다.");
				}

				_sessions[session.SessionId] = session;
				_logger.LogDebug("세션을 수정했습니다: {SessionId}", session.SessionId);
				return Task.FromResult(session);
			}
		}

		/// <summary>
		/// 지정한 세션 ID에 해당하는 세션을 저장소에서 제거합니다.
		/// </summary>
		/// <param name="sessionId">삭제할 세션의 식별자(세션 ID).</param>
		public Task DeleteAsync(string sessionId)
		{
			lock (_lock)
			{
				if (_sessions.Remove(sessionId))
				{
					_logger.LogDebug("세션을 삭제했습니다: {SessionId}", sessionId);
				}
				else
				{
					_logger.LogWarning("세션 ID {SessionId}를 삭제하려 했지만 세션을 찾을 수 없습니다", sessionId);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// 지정한 세션 ID가 저장소에 존재하는지 비동기적으로 확인합니다. 내부적으로 동기화되어 스레드 안전하게 동작합니다.
		/// </summary>
		/// <param name="sessionId">확인할 세션의 ID.</param>
		/// <returns>세션이 존재하면 <c>true</c>, 존재하지 않으면 <c>false</c>를 반환하는 <see cref="Task{Boolean}"/>.</returns>
		public Task<bool> ExistsAsync(string sessionId)
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.ContainsKey(sessionId));
			}
		}

		/// <summary>
		/// 현재 메모리에 저장된 활성 세션 수를 비동기적으로 반환합니다 (스레드 안전).
		/// </summary>
		/// <returns>메모리 저장소에 보관된 세션의 총 개수를 나타내는 <see cref="Task{Int32}"/>.</returns>
		public Task<int> GetActiveSessionCountAsync()
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.Count);
			}
		}

		/// <summary>
		/// 지정한 사용자 ID에 연관된 세션들을 반환합니다.
		/// </summary>
		/// <remarks>
		/// null 또는 빈 문자열이 전달되면 빈 열거형을 반환합니다. 메서드는 내부 저장소에서 해당 UserId와 일치하는 SessionInfo 객체들을 필터링하여 반환합니다. 이 구현은 인메모리 저장소에서 안전하게 호출될 수 있도록 동기화되어 있습니다.
		/// </remarks>
		/// <param name="userId">조회할 사용자 ID. null 또는 빈 값이면 결과는 비어 있습니다.</param>
		/// <returns>해당 사용자 ID에 연결된 SessionInfo 객체들의 비동기 열거형(Task<IEnumerable&lt;SessionInfo&gt;>).</returns>
		public Task<IEnumerable<SessionInfo>> GetSessionsByUserIdAsync(string? userId)
		{
			lock (_lock)
			{
				if (string.IsNullOrEmpty(userId))
				{
					return Task.FromResult(Enumerable.Empty<SessionInfo>());
				}

				var result = _sessions.Values.Where(s => s.UserId == userId).AsEnumerable();
				return Task.FromResult(result);
			}
		}
	}
}


