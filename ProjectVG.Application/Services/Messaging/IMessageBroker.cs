using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.Messaging
{
	public interface IMessageBroker
	{
		/// <summary>
		/// 텍스트 메시지를 전송합니다
		/// </summary>
		Task SendTextAsync(string sessionId, string message);

		/// <summary>
		/// 바이너리 데이터를 전송합니다
		/// </summary>
		Task SendBinaryAsync(string sessionId, byte[] data);

		/// <summary>
		/// WebSocket 메시지를 전송합니다
		/// </summary>
		Task SendWebSocketMessageAsync(string sessionId, WebSocketMessage message);
	}
}


