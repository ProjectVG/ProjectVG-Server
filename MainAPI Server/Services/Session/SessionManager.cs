using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MainAPI_Server.Services.Session
{
    public class SessionManager
    {
        private static readonly ConcurrentDictionary<string, ClientConnection> _sessions = new();

        /// <summary>
        /// 새로운 세션 등록
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        /// <param name="socket">소캣</param>
        public static void Register(string sessionId, WebSocket socket, string? userId = null)
        {
            _sessions[sessionId] = new ClientConnection {
                SessionId = sessionId,
                Socket = socket,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 세션 제거
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        public static void Unregister(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }

        /// <summary>
        /// 지정된 클라이언트 세션으로 메시지 전송
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        /// <param name="message">전송할 메시지</param>
        /// <returns></returns>
        public static async Task SendToClientAsync(string sessionId, string message)
        {
            if (!_sessions.TryGetValue(sessionId, out var conn)) return;
            if (conn.Socket.State != WebSocketState.Open) return;

            var buffer = Encoding.UTF8.GetBytes(message);
            await conn.Socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        /// <summary>
        /// 모든 세션 참조
        /// </summary>
        /// <returns>모든 클라이언트 세션</returns>
        public static IEnumerable<ClientConnection> GetAll() => _sessions.Values;

    }
}
