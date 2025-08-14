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

		public Task<SessionInfo> CreateAsync(SessionInfo session)
		{
			lock (_lock)
			{
				if (_sessions.ContainsKey(session.SessionId))
				{
					throw new InvalidOperationException($"세션 ID '{session.SessionId}'가 이미 존재합니다.");
				}

				_sessions[session.SessionId] = session;
				_logger.LogDebug("세션을 생성했습니다: {SessionId}", session.SessionId);
				return Task.FromResult(session);
			}
		}

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


