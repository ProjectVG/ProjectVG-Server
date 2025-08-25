using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProjectVG.Infrastructure.Auth
{
    public class RedisRefreshTokenStorage : IRefreshTokenStorage
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisRefreshTokenStorage> _logger;
        private const string KeyPrefix = "refresh_token:";

        public RedisRefreshTokenStorage(IConnectionMultiplexer redis, ILogger<RedisRefreshTokenStorage> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var expiry = expiresAt - DateTime.UtcNow;
                
                return await db.StringSetAsync(key, userId.ToString(), expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store refresh token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var value = await db.StringGetAsync(key);
                
                if (value.HasValue && Guid.TryParse(value, out var userId))
                {
                    return userId;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID from refresh token");
                return null;
            }
        }

        public async Task<bool> RemoveRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                return await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove refresh token");
                return false;
            }
        }

        public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                return await db.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check refresh token validity");
                return false;
            }
        }

        public async Task<DateTime?> GetRefreshTokenExpiresAtAsync(string refreshToken)
        {
            try
            {
                var db = _redis.GetDatabase();
                var key = $"{KeyPrefix}{refreshToken}";
                var timeToLive = await db.KeyTimeToLiveAsync(key);
                
                if (timeToLive.HasValue)
                {
                    return DateTime.UtcNow.Add(timeToLive.Value);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get refresh token expiration time");
                return null;
            }
        }
    }
}
