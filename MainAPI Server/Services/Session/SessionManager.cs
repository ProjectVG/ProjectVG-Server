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
            Console.WriteLine($"[SessionManager] 클라이언트에게 메시지 전송 시도: 세션ID={sessionId}");
            
            if (!_sessions.TryGetValue(sessionId, out var conn))
            {
                Console.WriteLine($"[SessionManager] 세션을 찾을 수 없음: {sessionId}");
                return;
            }
            
            Console.WriteLine($"[SessionManager] 세션 발견, WebSocket 상태: {conn.Socket.State}");
            
            if (conn.Socket.State != WebSocketState.Open)
            {
                Console.WriteLine($"[SessionManager] WebSocket이 열려있지 않음. 상태: {conn.Socket.State}");
                return;
            }

            Console.WriteLine($"[SessionManager] 메시지 전송 중: {message}");
            var buffer = Encoding.UTF8.GetBytes(message);
            await conn.Socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            Console.WriteLine("[SessionManager] 메시지 전송 완료");
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
