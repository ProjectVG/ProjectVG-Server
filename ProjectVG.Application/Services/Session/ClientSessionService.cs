using ProjectVG.Domain.Entities.Session;
using ProjectVG.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Session
{
    public class ClientSessionService : IClientSessionService
    {
        private readonly IClientSessionRepository _sessionRepository;
        private readonly ILogger<ClientSessionService> _logger;

        public ClientSessionService(IClientSessionRepository sessionRepository, ILogger<ClientSessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _logger = logger;
        }

        public async Task<ClientSession> CreateSessionAsync(Guid userId, string clientIP, int clientPort)
        {
            try
            {
                var sessionId = GenerateSessionId();
                var session = new ClientSession
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ClientIP = clientIP,
                    ClientPort = clientPort,
                    IsActive = true,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                var createdSession = await _sessionRepository.CreateAsync(session);
                
                _logger.LogInformation("새 세션을 생성했습니다. 세션 ID: {SessionId}, 사용자 ID: {UserId}", 
                    sessionId, userId);
                
                return createdSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 생성 중 오류가 발생했습니다. 사용자 ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<ClientSession?> GetSessionAsync(string sessionId)
        {
            try
            {
                var session = await _sessionRepository.GetBySessionIdAsync(sessionId);
                if (session != null && session.IsExpired())
                {
                    _logger.LogWarning("만료된 세션 접근 시도: {SessionId}", sessionId);
                    await DeleteSessionAsync(sessionId);
                    return null;
                }
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 조회 중 오류가 발생했습니다. 세션 ID: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<ClientSession?> GetSessionByUserIdAsync(Guid userId)
        {
            try
            {
                var session = await _sessionRepository.GetByUserIdAsync(userId);
                if (session != null && session.IsExpired())
                {
                    _logger.LogWarning("만료된 세션 접근 시도. 사용자 ID: {UserId}", userId);
                    await DeleteSessionAsync(session.SessionId);
                    return null;
                }
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자별 세션 조회 중 오류가 발생했습니다. 사용자 ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<ClientSession?> GetSessionByConnectionIdAsync(string connectionId)
        {
            try
            {
                var session = await _sessionRepository.GetByConnectionIdAsync(connectionId);
                if (session != null && session.IsExpired())
                {
                    _logger.LogWarning("만료된 세션 접근 시도. 연결 ID: {ConnectionId}", connectionId);
                    await DeleteSessionAsync(session.SessionId);
                    return null;
                }
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "연결 ID별 세션 조회 중 오류가 발생했습니다. 연결 ID: {ConnectionId}", connectionId);
                throw;
            }
        }

        public async Task<ClientSession> UpdateSessionAsync(ClientSession session)
        {
            try
            {
                var updatedSession = await _sessionRepository.UpdateAsync(session);
                _logger.LogDebug("세션을 수정했습니다. 세션 ID: {SessionId}", session.SessionId);
                return updatedSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 수정 중 오류가 발생했습니다. 세션 ID: {SessionId}", session.SessionId);
                throw;
            }
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            try
            {
                await _sessionRepository.DeleteAsync(sessionId);
                _logger.LogInformation("세션을 삭제했습니다. 세션 ID: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 삭제 중 오류가 발생했습니다. 세션 ID: {SessionId}", sessionId);
                throw;
            }
        }

        public async Task DeleteSessionsByUserIdAsync(Guid userId)
        {
            try
            {
                await _sessionRepository.DeleteByUserIdAsync(userId);
                _logger.LogInformation("사용자의 모든 세션을 삭제했습니다. 사용자 ID: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 세션 삭제 중 오류가 발생했습니다. 사용자 ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateSessionAsync(string sessionId)
        {
            try
            {
                var session = await GetSessionAsync(sessionId);
                return session != null && session.IsActive && !session.IsExpired();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "세션 유효성 검사 중 오류가 발생했습니다. 세션 ID: {SessionId}", sessionId);
                return false;
            }
        }

        private string GenerateSessionId()
        {
            return $"session_{Guid.NewGuid():N}";
        }
    }
} 