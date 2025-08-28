using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Infrastructure.Auth
{
    public class InMemoryRefreshTokenStorage : IRefreshTokenStorage
    {
        private readonly Dictionary<string, (Guid UserId, DateTime ExpiresAt)> _refreshTokens = new();
        private readonly ILogger<InMemoryRefreshTokenStorage> _logger;
        private readonly object _lock = new object();

        public InMemoryRefreshTokenStorage(ILogger<InMemoryRefreshTokenStorage> logger)
        {
            _logger = logger;
        }

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
