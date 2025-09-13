using ProjectVG.Common.Models.Session;
using System.Buffers;
using System.Text;
using System.Net.WebSockets;

namespace ProjectVG.Infrastructure.Realtime.WebSocketConnection
{
	/// <summary>
	/// WebSocket 기반의 클라이언트 연결 구현체입니다
	/// </summary>
	public class WebSocketClientConnection : IClientConnection
	{
		private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
		private static readonly Encoding _utf8Encoding = Encoding.UTF8;

		public string UserId { get; set; } = string.Empty;
		public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
		public System.Net.WebSockets.WebSocket WebSocket { get; set; } = null!;

		public WebSocketClientConnection(string userId, WebSocket socket)
		{
			UserId = userId;
			WebSocket = socket;
			ConnectedAt = DateTime.UtcNow;
		}

		/// <summary>
		/// 텍스트 메시지를 전송합니다 (ArrayPool 사용으로 LOH 할당 방지)
		/// </summary>
		public async Task SendTextAsync(string message)
		{
			byte[]? rentedBuffer = null;
			try
			{
				// UTF8 인코딩에 필요한 최대 바이트 수 계산
				var maxByteCount = _utf8Encoding.GetMaxByteCount(message.Length);
				rentedBuffer = _arrayPool.Rent(maxByteCount);

				// 실제 인코딩된 바이트 수
				var actualByteCount = _utf8Encoding.GetBytes(message, 0, message.Length, rentedBuffer, 0);

				// ArraySegment 생성하여 실제 사용된 부분만 전송
				var segment = new ArraySegment<byte>(rentedBuffer, 0, actualByteCount);
				await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			finally
			{
				if (rentedBuffer != null)
				{
					_arrayPool.Return(rentedBuffer);
				}
			}
		}

		/// <summary>
		/// 바이너리 메시지를 전송합니다
		/// </summary>
		public Task SendBinaryAsync(byte[] data)
		{
			return WebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
		}

		/// <summary>
		/// 청크 방식으로 대용량 바이너리 데이터를 전송합니다 (LOH 방지)
		/// </summary>
		public async Task SendLargeBinaryAsync(byte[] data)
		{
			const int chunkSize = 32768; // 32KB 청크
			var totalLength = data.Length;
			var offset = 0;

			while (offset < totalLength)
			{
				var remainingBytes = totalLength - offset;
				var currentChunkSize = Math.Min(chunkSize, remainingBytes);
				var isLastChunk = offset + currentChunkSize >= totalLength;

				var segment = new ArraySegment<byte>(data, offset, currentChunkSize);
				await WebSocket.SendAsync(segment, WebSocketMessageType.Binary, isLastChunk, CancellationToken.None);

				offset += currentChunkSize;
			}
		}
	}
}
