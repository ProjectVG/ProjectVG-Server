using Microsoft.Extensions.Logging;
using ProjectVG.Common.Models.Session;
using System.Collections.Concurrent;

namespace ProjectVG.Application.Services.Session
{
    /// <summary>
    /// WebSocket 연결 관리자 - 로컬 WebSocket 연결 객체 관리 전용
    /// </summary>
    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ILogger<WebSocketConnectionManager> _logger;
        private readonly ConcurrentDictionary<string, IClientConnection> _connections;

        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
            _connections = new ConcurrentDictionary<string, IClientConnection>();
        }

        public void RegisterConnection(string sessionId, IClientConnection connection)
        {
            try
            {
                _connections[sessionId] = connection;

                _logger.LogInformation("[WebSocketConnectionManager] 로컬 WebSocket 연결 등록: SessionId={SessionId}, 총연결수={TotalConnections}",
                    sessionId, _connections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 연결 등록 실패: SessionId={SessionId}", sessionId);
                throw;
            }
        }

        public void UnregisterConnection(string sessionId)
        {
            try
            {
                if (_connections.TryRemove(sessionId, out var removedConnection))
                {
                    _logger.LogInformation("[WebSocketConnectionManager] 로컬 WebSocket 연결 해제: SessionId={SessionId}, 남은연결수={RemainingConnections}",
                        sessionId, _connections.Count);
                }
                else
                {
                    _logger.LogWarning("[WebSocketConnectionManager] 해제 대상 연결을 찾을 수 없음: SessionId={SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 연결 해제 실패: SessionId={SessionId}", sessionId);
            }
        }

        public bool HasLocalConnection(string sessionId)
        {
            try
            {
                var hasConnection = _connections.ContainsKey(sessionId);

                _logger.LogDebug("[WebSocketConnectionManager] 로컬 연결 확인: SessionId={SessionId}, HasConnection={HasConnection}",
                    sessionId, hasConnection);

                return hasConnection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 로컬 연결 확인 실패: SessionId={SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> SendTextAsync(string sessionId, string message)
        {
            try
            {
                if (_connections.TryGetValue(sessionId, out var connection) && connection != null)
                {
                    await connection.SendTextAsync(message);

                    _logger.LogDebug("[WebSocketConnectionManager] 텍스트 메시지 전송 성공: SessionId={SessionId}, MessageLength={MessageLength}",
                        sessionId, message?.Length ?? 0);

                    return true;
                }

                _logger.LogWarning("[WebSocketConnectionManager] 메시지 전송 실패 - 연결을 찾을 수 없음: SessionId={SessionId}", sessionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 텍스트 메시지 전송 실패: SessionId={SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> SendBinaryAsync(string sessionId, byte[] data)
        {
            try
            {
                if (_connections.TryGetValue(sessionId, out var connection) && connection != null)
                {
                    await connection.SendBinaryAsync(data);

                    _logger.LogDebug("[WebSocketConnectionManager] 바이너리 데이터 전송 성공: SessionId={SessionId}, DataLength={DataLength}",
                        sessionId, data?.Length ?? 0);

                    return true;
                }

                _logger.LogWarning("[WebSocketConnectionManager] 바이너리 전송 실패 - 연결을 찾을 수 없음: SessionId={SessionId}", sessionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 바이너리 데이터 전송 실패: SessionId={SessionId}", sessionId);
                return false;
            }
        }

        public int GetLocalConnectionCount()
        {
            try
            {
                var count = _connections.Count;

                _logger.LogDebug("[WebSocketConnectionManager] 로컬 연결 수: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 로컬 연결 수 조회 실패");
                return 0;
            }
        }

        public IEnumerable<string> GetLocalConnectedSessionIds()
        {
            try
            {
                var sessionIds = _connections.Keys.ToList();

                _logger.LogDebug("[WebSocketConnectionManager] 로컬 연결된 세션 ID 조회: {Count}개", sessionIds.Count);
                return sessionIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 로컬 연결된 세션 ID 조회 실패");
                return Enumerable.Empty<string>();
            }
        }

        public IClientConnection? GetConnection(string sessionId)
        {
            try
            {
                _connections.TryGetValue(sessionId, out var connection);

                _logger.LogDebug("[WebSocketConnectionManager] 연결 객체 조회: SessionId={SessionId}, Found={Found}",
                    sessionId, connection != null);

                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebSocketConnectionManager] 연결 객체 조회 실패: SessionId={SessionId}", sessionId);
                return null;
            }
        }
    }
}