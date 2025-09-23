using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models.Session;
using StackExchange.Redis;
using System.Text.Json;

namespace ProjectVG.Infrastructure.Persistence.Session
{
	public class RedisSessionStorage : ISessionStorage
	{
		private readonly IConnectionMultiplexer _redis;
		private readonly IDatabase _database;
		private readonly ILogger<RedisSessionStorage> _logger;

		private const string SESSION_KEY_PREFIX = "session:user:";
		private static readonly TimeSpan SESSION_TTL = TimeSpan.FromMinutes(30);

		public RedisSessionStorage(IConnectionMultiplexer redis, ILogger<RedisSessionStorage> logger)
		{
			_redis = redis;
			_database = redis.GetDatabase();
			_logger = logger;
		}

		public async Task<SessionInfo?> GetAsync(string sessionId)
		{
			try
			{
				var key = GetSessionKey(sessionId);
				var value = await _database.StringGetAsync(key);

				if (!value.HasValue)
				{
					_logger.LogDebug("세션을 찾을 수 없음: {SessionId}", sessionId);
					return null;
				}

				var sessionInfo = JsonSerializer.Deserialize<SessionInfo>(value!);
				_logger.LogDebug("세션 조회 성공: {SessionId}", sessionId);
				return sessionInfo;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 조회 실패: {SessionId}", sessionId);
				return null;
			}
		}

		public async Task<IEnumerable<SessionInfo>> GetAllAsync()
		{
			try
			{
				var server = _redis.GetServer(_redis.GetEndPoints().First());
				var keys = server.Keys(pattern: SESSION_KEY_PREFIX + "*");
				var sessions = new List<SessionInfo>();

				foreach (var key in keys)
				{
					var value = await _database.StringGetAsync(key);
					if (value.HasValue)
					{
						var session = JsonSerializer.Deserialize<SessionInfo>(value!);
						if (session != null)
						{
							sessions.Add(session);
						}
					}
				}

				_logger.LogDebug("전체 세션 조회: {Count}개", sessions.Count);
				return sessions;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "전체 세션 조회 실패");
				return Enumerable.Empty<SessionInfo>();
			}
		}

		public async Task<SessionInfo> CreateAsync(SessionInfo session)
		{
			try
			{
				var key = GetSessionKey(session.SessionId);
				session.ConnectedAt = DateTime.UtcNow;
				session.LastActivity = DateTime.UtcNow;

				var json = JsonSerializer.Serialize(session);
				await _database.StringSetAsync(key, json, SESSION_TTL);

				_logger.LogInformation("세션 생성 성공: {SessionId}, TTL={TTL}분", session.SessionId, SESSION_TTL.TotalMinutes);
				return session;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 생성 실패: {SessionId}", session.SessionId);
				throw;
			}
		}

		public async Task<SessionInfo> UpdateAsync(SessionInfo session)
		{
			try
			{
				var key = GetSessionKey(session.SessionId);
				session.LastActivity = DateTime.UtcNow;

				var json = JsonSerializer.Serialize(session);
				await _database.StringSetAsync(key, json, SESSION_TTL);

				_logger.LogDebug("세션 업데이트 성공 (하트비트): {SessionId}", session.SessionId);
				return session;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 업데이트 실패: {SessionId}", session.SessionId);
				throw;
			}
		}

		public async Task DeleteAsync(string sessionId)
		{
			try
			{
				var key = GetSessionKey(sessionId);
				var deleted = await _database.KeyDeleteAsync(key);

				if (deleted)
				{
					_logger.LogInformation("세션 삭제 성공: {SessionId}", sessionId);
				}
				else
				{
					_logger.LogWarning("삭제할 세션을 찾을 수 없음: {SessionId}", sessionId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 삭제 실패: {SessionId}", sessionId);
				throw;
			}
		}

		public async Task<bool> ExistsAsync(string sessionId)
		{
			try
			{
				var key = GetSessionKey(sessionId);
				var exists = await _database.KeyExistsAsync(key);
				_logger.LogDebug("세션 존재 확인: {SessionId} = {Exists}", sessionId, exists);
				return exists;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 존재 확인 실패: {SessionId}", sessionId);
				return false;
			}
		}

		public async Task<int> GetActiveSessionCountAsync()
		{
			try
			{
				var server = _redis.GetServer(_redis.GetEndPoints().First());
				var keys = server.Keys(pattern: SESSION_KEY_PREFIX + "*");
				var count = keys.Count();

				_logger.LogDebug("활성 세션 수: {Count}", count);
				return count;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "활성 세션 수 조회 실패");
				return 0;
			}
		}

		public async Task<IEnumerable<SessionInfo>> GetSessionsByUserIdAsync(string? userId)
		{
			try
			{
				if (string.IsNullOrEmpty(userId))
					return Enumerable.Empty<SessionInfo>();

				var session = await GetAsync(userId);
				return session != null ? new[] { session } : Enumerable.Empty<SessionInfo>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "사용자 세션 조회 실패: {UserId}", userId);
				return Enumerable.Empty<SessionInfo>();
			}
		}

		/// <summary>
		/// 세션 하트비트 - TTL 갱신
		/// </summary>
		public async Task<bool> HeartbeatAsync(string sessionId)
		{
			try
			{
				var session = await GetAsync(sessionId);
				if (session == null)
				{
					_logger.LogWarning("하트비트: 세션을 찾을 수 없음: {SessionId}", sessionId);
					return false;
				}

				await UpdateAsync(session);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "세션 하트비트 실패: {SessionId}", sessionId);
				return false;
			}
		}

		private string GetSessionKey(string sessionId)
		{
			return SESSION_KEY_PREFIX + sessionId;
		}
	}
}


