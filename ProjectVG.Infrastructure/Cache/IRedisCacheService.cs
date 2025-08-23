namespace ProjectVG.Infrastructure.Cache;

public interface IRedisCacheService
{
    Task<string?> GetStringAsync(string key);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null);
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null);
    Task<long> DecrementAsync(string key, long value = 1, TimeSpan? expiry = null);
    Task<bool> HashSetAsync(string key, string field, string value);
    Task<string?> HashGetAsync(string key, string field);
    Task<bool> HashDeleteAsync(string key, string field);
    Task<Dictionary<string, string>> HashGetAllAsync(string key);
}
