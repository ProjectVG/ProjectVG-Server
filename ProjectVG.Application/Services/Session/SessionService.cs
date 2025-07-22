using ProjectVG.Infrastructure.Services.Session;
using ProjectVG.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace ProjectVG.Application.Services.Session
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<SessionService> _logger;
        private readonly ConcurrentDictionary<string, ClientConnection> _activeConnections = new();

        public SessionService(ISessionRepository sessionRepository, ILogger<SessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _logger = logger;
        }

        public async Task RegisterSessionAsync(string sessionId, WebSocket socket, string? userId = null)
        {
            try
            {
                var connection = new ClientConnection
                {
                    SessionId = sessionId,
                    WebSocket = socket,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow
                };

                await _sessionRepository.CreateAsync(connection);
                _logger.LogInformation("세션 등록 완료: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 등록 중 오류 발생: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task UnregisterSessionAsync(string sessionId)
        {
            try
            {
                await _sessionRepository.DeleteAsync(sessionId);
                _logger.LogInformation("세션 해제 완료: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 해제 중 오류 발생: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task SendMessageAsync(string sessionId, string message)
        {
            try
            {
                // Repository에서 세션 정보 조회
                var connection = await _sessionRepository.GetAsync(sessionId);
                if (connection == null)
                {
                    _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
                    return;
                }


                // 실제 WebSocket으로 메시지 전송
                var buffer = Encoding.UTF8.GetBytes(message);
                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                
                _logger.LogDebug("메시지 전송 완료: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 전송 실패: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task SendAudioAsync(string sessionId, byte[] audioData, string? contentType = null, float? audioLength = null)
        {
            try
            {
                var connection = await _sessionRepository.GetAsync(sessionId);
                if (connection == null)
                {
                    _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
                    return;
                }

                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(audioData),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None
                );

                _logger.LogInformation("오디오(wav) 전송 완료: {SessionId}, 바이트: {Length}", sessionId, audioData?.Length ?? 0);
                _logger.LogDebug("오디오(wav) 전송 완료: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "오디오(wav) 전송 실패: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<ClientConnection?> GetSessionAsync(string sessionId)
        {
            try
            {
                // 먼저 활성 연결에서 확인
                if (_activeConnections.TryGetValue(sessionId, out var activeConnection))
                {
                    _logger.LogDebug("활성 세션 조회 완료: {SessionId}", sessionId);
                    return activeConnection;
                }

                // Repository에서 조회
                var connection = await _sessionRepository.GetAsync(sessionId);
                _logger.LogDebug("저장된 세션 조회 완료: {SessionId}", sessionId);
                
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 조회 중 오류 발생: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<IEnumerable<ClientConnection>> GetAllSessionsAsync()
        {
            try
            {
                // 활성 연결과 저장된 세션을 모두 반환
                var activeSessions = _activeConnections.Values.ToList();
                var storedSessions = await _sessionRepository.GetAllAsync();
                
                // 중복 제거 (SessionId 기준)
                var allSessions = activeSessions.Concat(storedSessions)
                    .GroupBy(s => s.SessionId)
                    .Select(g => g.First())
                    .ToList();
                
                _logger.LogDebug("모든 세션 조회 완료: 활성 {ActiveCount}개, 저장된 {StoredCount}개", 
                    activeSessions.Count, storedSessions.Count());
                
                return allSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "모든 세션 조회 중 오류 발생");
                throw;
            }
        }

        public async Task<int> GetActiveSessionCountAsync()
        {
            try
            {
                // 활성 WebSocket 연결 수 반환
                var activeCount = _activeConnections.Count;
                _logger.LogDebug("활성 세션 수 조회 완료: {Count}개", activeCount);
                
                return activeCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "활성 세션 수 조회 중 오류 발생");
                throw;
            }
        }
    }
} 