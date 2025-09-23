using System.Collections.Concurrent;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
	public class ConnectionRegistry : IConnectionRegistry
	{
		private readonly ILogger<ConnectionRegistry> _logger;
		private readonly ConcurrentDictionary<string, IClientConnection> _connections = new();

		public ConnectionRegistry(ILogger<ConnectionRegistry> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// 연결을 등록합니다
		/// </summary>
		public void Register(string userId, IClientConnection connection)
		{
			_connections[userId] = connection;
			_logger.LogDebug("연결 등록: {UserId}", userId);
		}

		/// <summary>
		/// 연결을 해제합니다
		/// </summary>
		public void Unregister(string userId)
		{
			if (_connections.TryRemove(userId, out var removed))
			{
				_logger.LogDebug("연결 해제: {UserId}", userId);
			}
			else
			{
				_logger.LogDebug("해제 대상 세션을 찾을 수 없음: {UserId}", userId);
			}
		}

		/// <summary>
		/// 연결을 조회합니다
		/// </summary>
		public bool TryGet(string userId, out IClientConnection? connection)
		{
			var ok = _connections.TryGetValue(userId, out var conn);
			connection = conn;
			return ok;
		}

		/// <summary>
		/// 연결 상태를 확인합니다
		/// </summary>
		public bool IsConnected(string sessionId)
		{
			return _connections.ContainsKey(sessionId);
		}

		/// <summary>
		/// 활성 연결 수를 반환합니다
		/// </summary>
		public int GetActiveConnectionCount()
		{
			return _connections.Count;
		}
	}
}


