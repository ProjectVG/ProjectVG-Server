using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Persistence.Session
{
	public class InMemorySessionStorage : ISessionStorage
	{
		private readonly Dictionary<string, SessionInfo> _sessions = new();
		private readonly ILogger<InMemorySessionStorage> _logger;
		private readonly object _lock = new();

		public InMemorySessionStorage(ILogger<InMemorySessionStorage> logger)
		{
			_logger = logger;
		}

		public Task<SessionInfo?> GetAsync(string sessionId)
		{
			lock (_lock)
			{
				_sessions.TryGetValue(sessionId, out var session);
				return Task.FromResult(session);
			}
		}

		public Task<IEnumerable<SessionInfo>> GetAllAsync()
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.Values.AsEnumerable());
			}
		}

		/// <summary>
		/// 메모리 내에 주어진 세션을 저장하거나 동일한 ID의 기존 세션을 교체하고 저장된 세션을 반환합니다.
		/// </summary>
		/// <param name="session">저장할 SessionInfo 객체 (SessionId를 키로 사용).</param>
		/// <returns>저장된 SessionInfo를 감싼 Task.</returns>
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
				_logger.LogDebug("세션을 생성했습니다: {UserId}", session.SessionId);
				return Task.FromResult(session);
			}
		}

		/// <summary>
		/// 지정한 세션 정보를 저장소의 동일한 SessionId 항목으로 교체하여 업데이트합니다.
		/// </summary>
		/// <remarks>
		/// 호출은 내부 잠금을 사용해 스레드 안전하게 수행됩니다.
		/// </remarks>
		/// <param name="session">교체할 세션 정보(유효한 SessionId 필드 포함).</param>
		/// <returns>업데이트된 세션을 포함하는 완료된 <see cref="Task{SessionInfo}"/>.</returns>
		/// <exception cref="KeyNotFoundException">지정한 <c>session.SessionId</c>에 해당하는 세션이 존재하지 않을 경우 발생합니다.</exception>
		public Task<SessionInfo> UpdateAsync(SessionInfo session)
		{
			lock (_lock)
			{
				if (!_sessions.ContainsKey(session.SessionId))
				{
					throw new KeyNotFoundException($"세션 ID {session.SessionId}를 찾을 수 없습니다.");
				}

				_sessions[session.SessionId] = session;
				_logger.LogDebug("세션을 수정했습니다: {UserId}", session.SessionId);
				return Task.FromResult(session);
			}
		}

		/// <summary>
		/// 지정된 세션 ID로 메모리 저장소에서 세션을 제거합니다.
		/// </summary>
		/// <param name="sessionId">제거할 세션의 식별자.</param>
		/// <remarks>
		/// 존재하지 않는 세션 ID를 지정해도 예외는 발생하지 않으며, 해당 경우 경고가 기록됩니다.
		/// 이 메서드는 내부 동기화를 사용해 스레드 안전하게 동작합니다.
		/// </remarks>
		/// <returns>작업 완료를 나타내는 완료된 <see cref="Task"/>.</returns>
		public Task DeleteAsync(string sessionId)
		{
			lock (_lock)
			{
				if (_sessions.Remove(sessionId))
				{
					_logger.LogDebug("세션을 삭제했습니다: {UserId}", sessionId);
				}
				else
				{
					_logger.LogWarning("세션 ID {UserId}를 삭제하려 했지만 세션을 찾을 수 없습니다", sessionId);
				}
			}
			return Task.CompletedTask;
		}

		public Task<bool> ExistsAsync(string sessionId)
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.ContainsKey(sessionId));
			}
		}

		public Task<int> GetActiveSessionCountAsync()
		{
			lock (_lock)
			{
				return Task.FromResult(_sessions.Count);
			}
		}

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


