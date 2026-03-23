using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة كاش موحدة تدعم Redis مع Fallback تلقائي إلى MemoryCache
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public RedisCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // محاولة القراءة من Redis
            var data = await _distributedCache.GetStringAsync(key);
            if (data is not null)
                return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            // في حالة فشل Redis → Fall back إلى MemoryCache
            _logger.LogWarning(ex, "Redis unavailable. Falling back to MemoryCache for key: {Key}", key);

            if (_memoryCache.TryGetValue(key, out T? cachedValue))
                return cachedValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        };

        try
        {
            await _distributedCache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            // Fallback إلى MemoryCache
            _logger.LogWarning(ex, "Redis unavailable. Writing to MemoryCache for key: {Key}", key);
            _memoryCache.Set(key, value, expiration ?? TimeSpan.FromMinutes(30));
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Removing from MemoryCache for key: {Key}", key);
            _memoryCache.Remove(key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }
}
