using System.Net.WebSockets;
using ProjectVG.Common.Models.Session;
using System.Text;

namespace ProjectVG.Infrastructure.Connections
{
	/// <summary>
	/// WebSocket 기반의 클라이언트 연결 구현체입니다
	/// </summary>
	public class WebSocketClientConnection : IClientConnection
	{
		public string SessionId { get; set; } = string.Empty;
		public string? UserId { get; set; }
		public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
		public WebSocket WebSocket { get; set; } = default;

		/// <summary>
		/// 텍스트 메시지를 전송합니다
		/// </summary>
		public Task SendTextAsync(string message)
		{
			var buffer = Encoding.UTF8.GetBytes(message);
			return WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
		}

		/// <summary>
		/// 바이너리 메시지를 전송합니다
		/// </summary>
		public Task SendBinaryAsync(byte[] data)
		{
			return WebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
		}
	}
}


