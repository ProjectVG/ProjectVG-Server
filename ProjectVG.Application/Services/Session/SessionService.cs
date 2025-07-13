using ProjectVG.Infrastructure.Services.Session;
using ProjectVG.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Session
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<SessionService> _logger;

        public SessionService(ISessionRepository sessionRepository, ILogger<SessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _logger = logger;
        }

        public async Task RegisterSessionAsync(string sessionId, string? userId = null)
        {
            try
            {
                var connection = new ClientConnection
                {
                    SessionId = sessionId,
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
                var connection = await _sessionRepository.GetAsync(sessionId);
                if (connection == null)
                {
                    _logger.LogWarning("세션을 찾을 수 없음: {SessionId}", sessionId);
                    return;
                }

                // WebSocket을 통한 메시지 전송 로직
                // 실제 구현에서는 WebSocket 연결을 통해 메시지를 전송
                _logger.LogDebug("메시지 전송 완료: 세션 {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메시지 전송 중 오류 발생: 세션 {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<ClientConnection?> GetSessionAsync(string sessionId)
        {
            try
            {
                var connection = await _sessionRepository.GetAsync(sessionId);
                _logger.LogDebug("세션 조회 완료: {SessionId}", sessionId);
                
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
                var sessions = await _sessionRepository.GetAllAsync();
                _logger.LogDebug("모든 세션 조회 완료: {Count}개", sessions.Count());
                
                return sessions;
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
                var count = await _sessionRepository.GetActiveSessionCountAsync();
                _logger.LogDebug("활성 세션 수 조회 완료: {Count}개", count);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "활성 세션 수 조회 중 오류 발생");
                throw;
            }
        }
    }
} 