using ProjectVG.Common.Models.Session;
using System.Text;

namespace ProjectVG.Infrastructure.Realtime.WebSocketConnection
{
	/// <summary>
	/// WebSocket 기반의 클라이언트 연결 구현체입니다
	/// </summary>
	public class WebSocketClientConnection : IClientConnection
	{
		public string SessionId { get; set; } = string.Empty;
		public string? UserId { get; set; }
		public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
		public System.Net.WebSockets.WebSocket WebSocket { get; set; } = null!;

		/// <summary>
		/// 지정된 세션 ID와 웹소켓으로 클라이언트 연결을 초기화합니다.
		/// </summary>
		/// <param name="sessionId">연결에 할당할 고유 세션 식별자.</param>
		/// <param name="userId">연결된 사용자의 선택적 식별자(없을 수 있음).</param>
		/// <remarks>
		/// 생성 시 ConnectedAt는 현재 UTC 시각으로 설정됩니다. 내부적으로 전달된 WebSocket 인스턴스가 연결의 전송에 사용됩니다.
		/// </remarks>
		public WebSocketClientConnection(string sessionId, System.Net.WebSockets.WebSocket socket, string? userId = null)
		{
			SessionId = sessionId;
			WebSocket = socket;
			UserId = userId;
			ConnectedAt = DateTime.UtcNow;
		}

		/// <summary>
		/// 텍스트 메시지를 전송합니다
		/// <summary>
		/// WebSocket을 통해 전달할 텍스트 메시지를 UTF-8로 인코딩하여 비동기 전송합니다.
		/// </summary>
		/// <param name="message">전송할 텍스트 메시지.</param>
		/// <returns>전송 작업을 나타내는 비동기 Task.</returns>
		public Task SendTextAsync(string message)
		{
			var buffer = Encoding.UTF8.GetBytes(message);
			return WebSocket.SendAsync(new ArraySegment<byte>(buffer), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
		}

		/// <summary>
		/// 바이너리 메시지를 전송합니다
		/// <summary>
		/// 지정한 바이트 배열을 단일 바이너리 WebSocket 메시지로(endOfMessage=true) 전송합니다.
		/// </summary>
		/// <param name="data">전송할 바이트 배열.</param>
		/// <returns>전송 작업을 나타내는 Task.</returns>
		public Task SendBinaryAsync(byte[] data)
		{
			return WebSocket.SendAsync(new ArraySegment<byte>(data), System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
		}
	}
}
