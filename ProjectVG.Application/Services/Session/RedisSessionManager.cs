using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
    /// <summary>
    /// Redis 기반 세션 관리자 - 분산 환경 지원
    /// </summary>
    public class RedisSessionManager : ISessionManager
    {
        private readonly ISessionStorage _sessionStorage;
        private readonly ILogger<RedisSessionManager> _logger;

        public RedisSessionManager(ISessionStorage sessionStorage, ILogger<RedisSessionManager> logger)
        {
            _sessionStorage = sessionStorage;
            _logger = logger;
        }

        public async Task<string> CreateSessionAsync(Guid userId)
        {
            var userIdString = userId.ToString();

            try
            {
                _logger.LogInformation("[RedisSessionManager] 세션 생성 시작: UserId={UserId}", userId);

                var sessionInfo = new SessionInfo
                {
                    SessionId = userIdString,
                    UserId = userIdString,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };

                await _sessionStorage.CreateAsync(sessionInfo);

                _logger.LogInformation("[RedisSessionManager] 세션 생성 완료: UserId={UserId}", userId);
                return userIdString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 세션 생성 실패: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsSessionActiveAsync(Guid userId)
        {
            var userIdString = userId.ToString();

            try
            {
                var isActive = await _sessionStorage.ExistsAsync(userIdString);

                _logger.LogInformation("[RedisSessionManager] 세션 상태 확인: UserId={UserId}, IsActive={IsActive}",
                    userId, isActive);

                return isActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 세션 상태 확인 실패: UserId={UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateSessionHeartbeatAsync(Guid userId)
        {
            var userIdString = userId.ToString();

            try
            {
                var sessionInfo = await _sessionStorage.GetAsync(userIdString);
                if (sessionInfo == null)
                {
                    _logger.LogWarning("[RedisSessionManager] 하트비트 업데이트: 세션을 찾을 수 없음: UserId={UserId}", userId);
                    return false;
                }

                sessionInfo.LastActivity = DateTime.UtcNow;
                await _sessionStorage.UpdateAsync(sessionInfo);

                _logger.LogDebug("[RedisSessionManager] 세션 하트비트 업데이트 완료: UserId={UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 세션 하트비트 업데이트 실패: UserId={UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteSessionAsync(Guid userId)
        {
            var userIdString = userId.ToString();

            try
            {
                await _sessionStorage.DeleteAsync(userIdString);

                _logger.LogInformation("[RedisSessionManager] 세션 삭제 완료: UserId={UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 세션 삭제 실패: UserId={UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetActiveSessionCountAsync()
        {
            try
            {
                var count = await _sessionStorage.GetActiveSessionCountAsync();

                _logger.LogDebug("[RedisSessionManager] 활성 세션 수: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 활성 세션 수 조회 실패");
                return 0;
            }
        }

        public async Task<IEnumerable<string>> GetActiveUserIdsAsync()
        {
            try
            {
                var sessions = await _sessionStorage.GetAllAsync();
                var userIds = sessions.Select(s => s.UserId).Where(id => !string.IsNullOrEmpty(id)).ToList();

                _logger.LogDebug("[RedisSessionManager] 활성 사용자 ID 조회: {Count}개", userIds.Count);
                return userIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RedisSessionManager] 활성 사용자 ID 조회 실패");
                return Enumerable.Empty<string>();
            }
        }
    }
}