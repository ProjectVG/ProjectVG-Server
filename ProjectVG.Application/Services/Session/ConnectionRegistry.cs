using System.Collections.Concurrent;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public class ConnectionRegistry : IConnectionRegistry
	{
		private readonly ILogger<ConnectionRegistry> _logger;
		private readonly ConcurrentDictionary<string, IClientConnection> _connections = new();
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userIdToSessionIds = new();

		public ConnectionRegistry(ILogger<ConnectionRegistry> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 연결을 등록합니다
		/// </summary>
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
		/// </summary>
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
		/// </summary>
		public bool TryGet(string sessionId, out IClientConnection? connection)
		{
			var ok = _connections.TryGetValue(sessionId, out var conn);
			connection = conn;
			return ok;
		}

		/// <summary>
		/// 사용자 ID로 세션 ID 목록을 조회합니다
		/// </summary>
		public IEnumerable<string> GetSessionIdsByUserId(string userId)
		{
			if (_userIdToSessionIds.TryGetValue(userId, out var set))
			{
				return set.Keys;
			}
			return Enumerable.Empty<string>();
		}

		/// <summary>
		/// 텍스트 메시지를 전송합니다
		/// </summary>
		public async Task SendTextAsync(string sessionId, string message)
		{
			if (TryGetConnection(sessionId, out var connection))
			{
				await connection.SendTextAsync(message);
			}
		}

		/// <summary>
		/// 바이너리 데이터를 전송합니다
		/// </summary>
		public async Task SendBinaryAsync(string sessionId, byte[] data)
		{
			if (TryGetConnection(sessionId, out var connection))
			{
				await connection.SendBinaryAsync(data);
			}
		}

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


