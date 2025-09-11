using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace ProjectVG.Tests.Application.TestUtilities
{
    /// <summary>
    /// Mock implementation of Memory service client for testing scenarios
    /// </summary>
    public class MockMemoryClient
    {
        private readonly Dictionary<string, MemoryData> _memoryStore;
        private readonly List<MemoryOperation> _operations;

        public MockMemoryClient()
        {
            _memoryStore = new Dictionary<string, MemoryData>();
            _operations = new List<MemoryOperation>();
        }

        public List<MemoryOperation> Operations => _operations.AsReadOnly().ToList();
        public int OperationCount => _operations.Count;

        public async Task<string?> GetMemoryContextAsync(Guid userId, Guid characterId)
        {
            _operations.Add(new MemoryOperation("GetMemoryContext", userId, characterId));

            var key = $"{userId}:{characterId}";
            if (_memoryStore.TryGetValue(key, out var memory))
            {
                return memory.Context;
            }

            return null;
        }

        public async Task StoreMemoryAsync(Guid userId, Guid characterId, string context, string summary)
        {
            _operations.Add(new MemoryOperation("StoreMemory", userId, characterId, context));

            var key = $"{userId}:{characterId}";
            _memoryStore[key] = new MemoryData
            {
                UserId = userId,
                CharacterId = characterId,
                Context = context,
                Summary = summary,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task UpdateMemoryAsync(Guid userId, Guid characterId, string newContext)
        {
            _operations.Add(new MemoryOperation("UpdateMemory", userId, characterId, newContext));

            var key = $"{userId}:{characterId}";
            if (_memoryStore.TryGetValue(key, out var existing))
            {
                existing.Context = newContext;
                existing.LastUpdated = DateTime.UtcNow;
            }
        }

        public async Task<List<string>> GetRelevantMemoriesAsync(Guid userId, Guid characterId, string query, int limit = 5)
        {
            _operations.Add(new MemoryOperation("GetRelevantMemories", userId, characterId, query));

            var key = $"{userId}:{characterId}";
            if (_memoryStore.TryGetValue(key, out var memory))
            {
                // Simple mock: return parts of stored context
                var parts = memory.Context?.Split('.', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                return parts.Take(limit).ToList();
            }

            return new List<string>();
        }

        public async Task ClearMemoryAsync(Guid userId, Guid characterId)
        {
            _operations.Add(new MemoryOperation("ClearMemory", userId, characterId));

            var key = $"{userId}:{characterId}";
            _memoryStore.Remove(key);
        }

        public void Reset()
        {
            _memoryStore.Clear();
            _operations.Clear();
        }

        public bool HasMemory(Guid userId, Guid characterId)
        {
            var key = $"{userId}:{characterId}";
            return _memoryStore.ContainsKey(key);
        }

        public MemoryData? GetStoredMemory(Guid userId, Guid characterId)
        {
            var key = $"{userId}:{characterId}";
            return _memoryStore.TryGetValue(key, out var memory) ? memory : null;
        }

        public MockMemoryClient WithPresetMemory(Guid userId, Guid characterId, string context, string summary = "")
        {
            var key = $"{userId}:{characterId}";
            _memoryStore[key] = new MemoryData
            {
                UserId = userId,
                CharacterId = characterId,
                Context = context,
                Summary = summary,
                LastUpdated = DateTime.UtcNow
            };
            return this;
        }
    }

    /// <summary>
    /// Mock implementation of IDistributedCache for testing scenarios
    /// </summary>
    public class MockDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly List<CacheOperation> _operations;

        public MockDistributedCache()
        {
            _cache = new Dictionary<string, CacheEntry>();
            _operations = new List<CacheOperation>();
        }

        public List<CacheOperation> Operations => _operations.AsReadOnly().ToList();

        public byte[]? Get(string key)
        {
            _operations.Add(new CacheOperation("Get", key));

            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                return entry.Value;
            }

            return null;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Get(key);
        }

        public string? GetString(string key)
        {
            var bytes = Get(key);
            return bytes != null ? Encoding.UTF8.GetString(bytes) : null;
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken token = default)
        {
            return GetString(key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions? options = null)
        {
            _operations.Add(new CacheOperation("Set", key, value));

            var expiration = DateTime.MaxValue;
            if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
            {
                expiration = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options?.AbsoluteExpiration.HasValue == true)
            {
                expiration = options.AbsoluteExpiration.Value.DateTime;
            }

            _cache[key] = new CacheEntry(value, expiration);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
        {
            Set(key, value, options);
        }

        public void SetString(string key, string value, DistributedCacheEntryOptions? options = null)
        {
            Set(key, Encoding.UTF8.GetBytes(value), options);
        }

        public async Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
        {
            SetString(key, value, options);
        }

        public void Refresh(string key)
        {
            _operations.Add(new CacheOperation("Refresh", key));
            // Mock implementation doesn't need to do anything for refresh
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            Refresh(key);
        }

        public void Remove(string key)
        {
            _operations.Add(new CacheOperation("Remove", key));
            _cache.Remove(key);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
        }

        public void Reset()
        {
            _cache.Clear();
            _operations.Clear();
        }

        public bool HasKey(string key)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired;
        }

        public int Count => _cache.Count(kvp => !kvp.Value.IsExpired);

        public MockDistributedCache WithPresetValue(string key, string value, TimeSpan? expiration = null)
        {
            var options = expiration.HasValue 
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }
                : null;
            SetString(key, value, options);
            return this;
        }

        public MockDistributedCache WithPresetObject<T>(string key, T obj, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(obj);
            return WithPresetValue(key, json, expiration);
        }

        public T? GetObject<T>(string key)
        {
            var json = GetString(key);
            return json != null ? JsonSerializer.Deserialize<T>(json) : default;
        }
    }

    /// <summary>
    /// Data models for mock services
    /// </summary>
    public class MemoryData
    {
        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public string? Context { get; set; }
        public string? Summary { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class MemoryOperation
    {
        public string Operation { get; }
        public Guid UserId { get; }
        public Guid CharacterId { get; }
        public string? Data { get; }
        public DateTime Timestamp { get; }

        public MemoryOperation(string operation, Guid userId, Guid characterId, string? data = null)
        {
            Operation = operation;
            UserId = userId;
            CharacterId = characterId;
            Data = data;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class CacheOperation
    {
        public string Operation { get; }
        public string Key { get; }
        public byte[]? Value { get; }
        public DateTime Timestamp { get; }

        public CacheOperation(string operation, string key, byte[]? value = null)
        {
            Operation = operation;
            Key = key;
            Value = value;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class CacheEntry
    {
        public byte[] Value { get; }
        public DateTime Expiration { get; }

        public CacheEntry(byte[] value, DateTime expiration)
        {
            Value = value;
            Expiration = expiration;
        }

        public bool IsExpired => DateTime.UtcNow > Expiration;
    }
}