using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MainAPI_Server.Services
{
    public class SessionManager
    {
        private static readonly ConcurrentDictionary<string, WebSocket> _sessions = new();

        /// <summary>
        /// 새로운 세션 등록
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        /// <param name="socket">소캣</param>
        public static void Register(string sessionId, WebSocket socket)
        {
            _sessions[sessionId] = socket;
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
            if (sessionId == null || !_sessions.ContainsKey(sessionId)) return;

            var socket = _sessions[sessionId];
            if (socket.State == WebSocketState.Open) {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }

    }
}
