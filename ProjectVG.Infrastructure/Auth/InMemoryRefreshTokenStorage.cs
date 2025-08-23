using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Infrastructure.Auth
{
    public class InMemoryRefreshTokenStorage : IRefreshTokenStorage
    {
        private readonly Dictionary<string, Guid> _refreshTokens = new();
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
                    _refreshTokens[refreshToken] = userId;
                }
                
                _logger.LogInformation("Refresh token stored for user {UserId}", userId);
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
                    if (_refreshTokens.TryGetValue(refreshToken, out var userId))
                    {
                        _logger.LogInformation("Found refresh token for user {UserId}", userId);
                        return Task.FromResult<Guid?>(userId);
                    }
                }
                
                _logger.LogWarning("Refresh token not found");
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
                    var isValid = _refreshTokens.ContainsKey(refreshToken);
                    _logger.LogInformation("Refresh token validation result: {IsValid}", isValid);
                    return Task.FromResult(isValid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate refresh token");
                return Task.FromResult(false);
            }
        }
    }
}
