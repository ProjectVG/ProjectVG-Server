using System.Net.WebSockets;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Infrastructure.Connections
{
	public class WebSocketClientConnectionFactory : IClientConnectionFactory
	{
		public IClientConnection Create(string sessionId, WebSocket socket, string? userId)
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


