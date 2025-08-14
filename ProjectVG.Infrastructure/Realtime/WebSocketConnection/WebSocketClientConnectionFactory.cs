using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Realtime.WebSocketConnection
{
	public class WebSocketClientConnectionFactory : IClientConnectionFactory
	{
		public IClientConnection Create(string sessionId, System.Net.WebSockets.WebSocket socket, string? userId)
		{
			return new WebSocketClientConnection
			{
				SessionId = sessionId,
				WebSocket = socket,
				UserId = userId,
				ConnectedAt = DateTime.UtcNow
			};
		}
	}
}
