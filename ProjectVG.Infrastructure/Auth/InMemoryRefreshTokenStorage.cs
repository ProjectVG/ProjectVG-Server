using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Infrastructure.Auth
{
    public class InMemoryRefreshTokenStorage : IRefreshTokenStorage
    {
        private readonly Dictionary<string, (Guid UserId, DateTime ExpiresAt)> _refreshTokens = new();
        private readonly ILogger<InMemoryRefreshTokenStorage> _logger;
        private readonly object _lock = new object();

        /// <summary>
        /// InMemoryRefreshTokenStorage의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 내부에서 토큰 관리 중 발생하는 로그를 기록하기 위해 로거를 설정합니다.
        /// </remarks>
        public InMemoryRefreshTokenStorage(ILogger<InMemoryRefreshTokenStorage> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 지정된 리프레시 토큰을 주어진 사용자 ID 및 만료 시간으로 메모리 저장소에 저장하거나 갱신합니다.
        /// </summary>
        /// <param name="refreshToken">저장할 리프레시 토큰 문자열.</param>
        /// <param name="userId">토큰에 연결할 사용자 식별자(Guid).</param>
        /// <param name="expiresAt">토큰의 만료 시각(UTC 기준). UTC로 제공되는 것이 권장됩니다.</param>
        /// <returns>저장이 성공하면 true, 예외 발생 등으로 실패하면 false를 반환합니다.</returns>
        public Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt)
        {
            try
            {
                lock (_lock)
                {
                    _refreshTokens[refreshToken] = (userId, expiresAt);
                }
                
                _logger.LogInformation("Refresh token stored for user {UserId} with expiration {ExpiresAt}", userId, expiresAt);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 주어진 리프레시 토큰에 연관된 사용자 ID를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 토큰이 존재하고 만료되지 않은 경우 해당 사용자의 <see cref="Guid"/>를 반환합니다.
        /// 만료된 토큰은 내부 저장소에서 제거됩니다. 시간 비교는 <see cref="DateTime.UtcNow"/>를 사용합니다.
        /// 오류 또는 토큰 미존재/만료 시에는 null을 반환합니다.
        /// </remarks>
        /// <returns>
        /// 토큰이 유효하면 사용자 ID(Guid), 그렇지 않으면 null을 반환합니다.
        /// </returns>
        public Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken)
        {
            try
            {
                lock (_lock)
                {
                    if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
                    {
                        // 만료 시간 확인
                        if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                        {
                            _logger.LogInformation("Found valid refresh token for user {UserId}", tokenInfo.UserId);
                            return Task.FromResult<Guid?>(tokenInfo.UserId);
                        }
                        else
                        {
                            // 만료된 토큰 제거
                            _refreshTokens.Remove(refreshToken);
                            _logger.LogWarning("Expired refresh token removed for user {UserId}", tokenInfo.UserId);
                        }
                    }
                }
                
                _logger.LogWarning("Refresh token not found or expired");
                return Task.FromResult<Guid?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID from refresh token");
                return Task.FromResult<Guid?>(null);
            }
        }

        /// <summary>
        /// 지정한 리프레시 토큰을 메모리 저장소에서 제거합니다.
        /// </summary>
        /// <param name="refreshToken">제거할 리프레시 토큰 문자열(키).</param>
        /// <returns>
        /// 토큰이 존재하여 제거되면 true, 토큰이 없거나 제거에 실패하면 false를 반환하는 비동기 작업(Task).
        /// 예외가 발생하면 내부에서 처리하고 false를 반환합니다.
        /// </returns>
        public Task<bool> RemoveRefreshTokenAsync(string refreshToken)
        {
            try
            {
                lock (_lock)
                {
                    var removed = _refreshTokens.Remove(refreshToken);
                    if (removed)
                    {
                        _logger.LogInformation("Refresh token removed");
                    }
                    return Task.FromResult(removed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove refresh token");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 지정한 리프레시 토큰이 존재하고 만료되지 않았는지 비동기적으로 확인합니다.
        /// </summary>
        /// <param name="refreshToken">검사할 리프레시 토큰 문자열.</param>
        /// <returns>
        /// 토큰이 존재하고 현재 UTC 시간 기준으로 만료되지 않았으면 <c>true</c>, 그렇지 않거나 오류가 발생하면 <c>false</c>를 반환합니다.
        /// </returns>
        /// <remarks>
        /// 만료된 토큰은 내부 저장소에서 제거됩니다. 내부 동기화를 위해 잠금(_lock)을 사용하며 시간 비교는 <see cref="DateTime.UtcNow"/>을 기준으로 합니다.
        /// 예외는 내부에서 처리되어 로그로 남기고 <c>false</c>를 반환합니다.
        /// </remarks>
        public Task<bool> IsRefreshTokenValidAsync(string refreshToken)
        {
            try
            {
                lock (_lock)
                {
                    if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
                    {
                        var isValid = tokenInfo.ExpiresAt > DateTime.UtcNow;
                        if (!isValid)
                        {
                            // 만료된 토큰 제거
                            _refreshTokens.Remove(refreshToken);
                        }
                        _logger.LogInformation("Refresh token validation result: {IsValid}", isValid);
                        return Task.FromResult(isValid);
                    }
                }
                
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate refresh token");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 지정한 리프레시 토큰의 만료 시각을 반환합니다.
        /// </summary>
        /// <param name="refreshToken">조회할 리프레시 토큰 문자열.</param>
        /// <returns>
        /// 토큰이 존재하고 만료되지 않았다면 해당 토큰의 만료 시각을 반환합니다(저장된 값, UTC 기준).  
        /// 토큰이 없거나 만료된 경우 null을 반환하며, 만료된 토큰은 내부 저장소에서 제거됩니다.
        /// </returns>
        public Task<DateTime?> GetRefreshTokenExpiresAtAsync(string refreshToken)
        {
            try
            {
                lock (_lock)
                {
                    if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
                    {
                        // 만료 시간 확인
                        if (tokenInfo.ExpiresAt > DateTime.UtcNow)
                        {
                            return Task.FromResult<DateTime?>(tokenInfo.ExpiresAt);
                        }
                        else
                        {
                            // 만료된 토큰 제거
                            _refreshTokens.Remove(refreshToken);
                        }
                    }
                }
                
                return Task.FromResult<DateTime?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get refresh token expiration time");
                return Task.FromResult<DateTime?>(null);
            }
        }
    }
}
