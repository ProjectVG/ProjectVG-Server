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
            Console.WriteLine($"SendToClientAsync called with sessionId: {sessionId}");
            
            if (!_sessions.TryGetValue(sessionId, out var conn))
            {
                Console.WriteLine($"Session not found: {sessionId}");
                return;
            }
            
            Console.WriteLine($"Session found, WebSocket state: {conn.Socket.State}");
            
            if (conn.Socket.State != WebSocketState.Open)
            {
                Console.WriteLine($"WebSocket is not open. State: {conn.Socket.State}");
                return;
            }

            Console.WriteLine($"Sending message: {message}");
            var buffer = Encoding.UTF8.GetBytes(message);
            await conn.Socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            Console.WriteLine("Message sent successfully");
        }

        /// <summary>
        /// 세션 조회
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>클라이언트 세션</returns>
        public static ClientConnection? Get(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var conn) ? conn : null;
        }

        /// <summary>
        /// 모든 세션 조회
        /// </summary>
        /// <returns>모든 클라이언트 세션</returns>
        public static IEnumerable<ClientConnection> GetAll() => _sessions.Values;

    }
}
