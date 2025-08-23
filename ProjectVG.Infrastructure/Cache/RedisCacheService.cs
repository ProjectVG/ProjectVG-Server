using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ProjectVG.Infrastructure.Cache;

public class RedisCacheService : IRedisCacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisCacheSettings _settings;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IOptions<RedisCacheSettings> settings)
    {
        _redis = redis;
        _settings = settings.Value;
        _database = redis.GetDatabase(_settings.Database);
        _logger = logger;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.StringGetAsync(fullKey);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting string from Redis for key: {Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            var fullKey = GetFullKey(key);
            await _database.StringSetAsync(fullKey, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string in Redis for key: {Key}", key);
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyDeleteAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from Redis: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence in Redis: {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.StringSetAsync(fullKey, value, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string if not exists in Redis for key: {Key}", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var result = await _database.StringIncrementAsync(fullKey, value);
            
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(fullKey, expiry);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key in Redis: {Key}", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, TimeSpan? expiry = null)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var result = await _database.StringDecrementAsync(fullKey, value);
            
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(fullKey, expiry);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing key in Redis: {Key}", key);
            return 0;
        }
    }

    public async Task<bool> HashSetAsync(string key, string field, string value)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.HashSetAsync(fullKey, field, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field in Redis for key: {Key}, field: {Field}", key, field);
            return false;
        }
    }

    public async Task<string?> HashGetAsync(string key, string field)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var value = await _database.HashGetAsync(fullKey, field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field from Redis for key: {Key}, field: {Field}", key, field);
            return null;
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field)
    {
        try
        {
            var fullKey = GetFullKey(key);
            return await _database.HashDeleteAsync(fullKey, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash field from Redis for key: {Key}, field: {Field}", key, field);
            return false;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        try
        {
            var fullKey = GetFullKey(key);
            var hashEntries = await _database.HashGetAllAsync(fullKey);
            
            return hashEntries.ToDictionary(
                entry => entry.Name.ToString(),
                entry => entry.Value.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields from Redis for key: {Key}", key);
            return new Dictionary<string, string>();
        }
    }

    private string GetFullKey(string key)
    {
        return string.IsNullOrEmpty(_settings.KeyPrefix) 
            ? key 
            : $"{_settings.KeyPrefix}:{key}";
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
