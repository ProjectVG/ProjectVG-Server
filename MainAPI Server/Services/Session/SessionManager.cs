using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MainAPI_Server.Services.Session
{
    public interface ISessionManager
    {
        void Register(string sessionId, WebSocket socket, string? userId = null);
        void Unregister(string sessionId);
        Task SendToClientAsync(string sessionId, string message);
        ClientConnection? Get(string sessionId);
        IEnumerable<ClientConnection> GetAll();
    }

    public class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, ClientConnection> _sessions = new();
        private readonly ILogger<SessionManager> _logger;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 새로운 세션 등록
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        /// <param name="socket">소캣</param>
        public void Register(string sessionId, WebSocket socket, string? userId = null)
        {
            _sessions[sessionId] = new ClientConnection {
                SessionId = sessionId,
                Socket = socket,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            };
            _logger.LogInformation("세션 등록됨: {SessionId}", sessionId);
        }

        /// <summary>
        /// 세션 제거
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        public void Unregister(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            _logger.LogInformation("세션 해제됨: {SessionId}", sessionId);
        }

        /// <summary>
        /// 지정된 클라이언트 세션으로 메시지 전송
        /// </summary>
        /// <param name="sessionId">세션 Id</param>
        /// <param name="message">전송할 메시지</param>
        /// <returns></returns>
        public async Task SendToClientAsync(string sessionId, string message)
        {
            _logger.LogDebug("클라이언트에게 메시지 전송 시도: 세션ID={SessionId}", sessionId);
            
            if (!_sessions.TryGetValue(sessionId, out var conn))
            {
                _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
                return;
            }
            
            _logger.LogDebug("세션 발견, WebSocket 상태: {SocketState}", conn.Socket.State);
            
            if (conn.Socket.State != WebSocketState.Open)
            {
                _logger.LogWarning("WebSocket이 열려있지 않음. 상태: {SocketState}, 세션: {SessionId}", conn.Socket.State, sessionId);
                return;
            }

            _logger.LogDebug("메시지 전송 중: {Message}", message);
            var buffer = Encoding.UTF8.GetBytes(message);
            await conn.Socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            _logger.LogDebug("메시지 전송 완료: {SessionId}", sessionId);
        }

        /// <summary>
        /// 세션 조회
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>클라이언트 세션</returns>
        public ClientConnection? Get(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var conn) ? conn : null;
        }

        /// <summary>
        /// 모든 세션 조회
        /// </summary>
        /// <returns>모든 클라이언트 세션</returns>
        public IEnumerable<ClientConnection> GetAll() => _sessions.Values;

    }
}
