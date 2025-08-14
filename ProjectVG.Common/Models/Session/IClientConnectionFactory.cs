using System.Net.WebSockets;

namespace ProjectVG.Common.Models.Session
{
	public interface IClientConnectionFactory
	{
		IClientConnection Create(string sessionId, WebSocket socket, string? userId);
	}
}


