using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Models.Session;
using ProjectVG.Application.Services.Session;
using System.Text.Json;

namespace ProjectVG.Application.Services.Messaging
{
	public class MessageBroker : IMessageBroker
	{
		private readonly IConnectionRegistry _connectionRegistry;
		private readonly ILogger<MessageBroker> _logger;

		public MessageBroker(IConnectionRegistry connectionRegistry, ILogger<MessageBroker> logger)
		{
			_connectionRegistry = connectionRegistry;
			_logger = logger;
		}

		/// <summary>
		/// 텍스트 메시지를 전송합니다
		/// </summary>
		public async Task SendTextAsync(string sessionId, string message)
		{
			await _connectionRegistry.SendTextAsync(sessionId, message);
			_logger.LogDebug("텍스트 전송: {SessionId}", sessionId);
		}

		/// <summary>
		/// 바이너리 데이터를 전송합니다
		/// </summary>
		public async Task SendBinaryAsync(string sessionId, byte[] data)
		{
			await _connectionRegistry.SendBinaryAsync(sessionId, data);
			_logger.LogDebug("바이너리 전송: {SessionId}, {Length} bytes", sessionId, data?.Length ?? 0);
		}

		/// <summary>
		/// WebSocket 메시지를 전송합니다
		/// </summary>
		public async Task SendWebSocketMessageAsync(string sessionId, WebSocketMessage message)
		{
			try
			{
				var json = JsonSerializer.Serialize(message);
				await _connectionRegistry.SendTextAsync(sessionId, json);
				_logger.LogInformation("WebSocket 메시지 전송 완료: {SessionId}, 타입: {MessageType}", sessionId, message.Type);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "WebSocket 메시지 전송 실패: {SessionId}", sessionId);
				throw;
			}
		}
	}
}


