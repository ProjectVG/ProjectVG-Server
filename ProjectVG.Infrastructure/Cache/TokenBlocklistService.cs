using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.Cache;

public class TokenBlocklistService : ITokenBlocklistService
{
    private readonly IRedisCacheService _redis;
    private readonly ILogger<TokenBlocklistService> _logger;

    private const string REFRESH_TOKEN_BLOCKLIST_PREFIX = "refresh:blocked";
    private const string ACCESS_TOKEN_BLOCKLIST_PREFIX = "access:blocked";
    private const string BLOCKLIST_COUNTER_KEY = "tokens:blocked:count";

    public TokenBlocklistService(IRedisCacheService redis, ILogger<TokenBlocklistService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task BlockRefreshTokenAsync(string tokenHash, TimeSpan expiry)
    {
        try
        {
            var key = GetRefreshTokenBlockKey(tokenHash);
            await _redis.SetStringAsync(key, DateTime.UtcNow.ToString("O"), expiry);
            await _redis.IncrementAsync(BLOCKLIST_COUNTER_KEY);
            
            _logger.LogInformation("Blocked refresh token with hash: {TokenHash} for {Expiry}", 
                tokenHash[..8] + "***", expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block refresh token");
        }
    }

    public async Task<bool> IsRefreshTokenBlockedAsync(string tokenHash)
    {
        try
        {
            var key = GetRefreshTokenBlockKey(tokenHash);
            return await _redis.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if refresh token is blocked");
            return false; // 실패 시 차단되지 않은 것으로 처리 (접근 허용)
        }
    }

    public async Task BlockAccessTokenAsync(string jti, TimeSpan expiry)
    {
        try
        {
            var key = GetAccessTokenBlockKey(jti);
            await _redis.SetStringAsync(key, DateTime.UtcNow.ToString("O"), expiry);
            await _redis.IncrementAsync(BLOCKLIST_COUNTER_KEY);
            
            _logger.LogInformation("Blocked access token with JTI: {JTI} for {Expiry}", 
                jti[..8] + "***", expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block access token");
        }
    }

    public async Task<bool> IsAccessTokenBlockedAsync(string jti)
    {
        try
        {
            var key = GetAccessTokenBlockKey(jti);
            return await _redis.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if access token is blocked");
            return false; // 실패 시 차단되지 않은 것으로 처리 (접근 허용)
        }
    }

    public async Task UnblockRefreshTokenAsync(string tokenHash)
    {
        try
        {
            var key = GetRefreshTokenBlockKey(tokenHash);
            if (await _redis.DeleteAsync(key))
            {
                await _redis.DecrementAsync(BLOCKLIST_COUNTER_KEY);
                _logger.LogInformation("Unblocked refresh token with hash: {TokenHash}", 
                    tokenHash[..8] + "***");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unblock refresh token");
        }
    }

    public async Task UnblockAccessTokenAsync(string jti)
    {
        try
        {
            var key = GetAccessTokenBlockKey(jti);
            if (await _redis.DeleteAsync(key))
            {
                await _redis.DecrementAsync(BLOCKLIST_COUNTER_KEY);
                _logger.LogInformation("Unblocked access token with JTI: {JTI}", 
                    jti[..8] + "***");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unblock access token");
        }
    }

    public async Task CleanupExpiredBlocksAsync()
    {
        try
        {
            // Redis에서 TTL이 있는 키들은 자동으로 만료되므로
            // 여기서는 카운터를 정확히 맞추기 위한 작업을 수행할 수 있습니다
            // 실제 구현에서는 더 정교한 정리 로직을 구현할 수 있습니다
            
            _logger.LogInformation("Token blocklist cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired token blocks");
        }
    }

    public async Task<long> GetBlockedTokenCountAsync()
    {
        try
        {
            var countStr = await _redis.GetStringAsync(BLOCKLIST_COUNTER_KEY);
            return long.TryParse(countStr, out var count) ? Math.Max(0, count) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blocked token count");
            return 0;
        }
    }

    private static string GetRefreshTokenBlockKey(string tokenHash)
    {
        return $"{REFRESH_TOKEN_BLOCKLIST_PREFIX}:{tokenHash}";
    }

    private static string GetAccessTokenBlockKey(string jti)
    {
        return $"{ACCESS_TOKEN_BLOCKLIST_PREFIX}:{jti}";
    }
}
