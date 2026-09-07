using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Services;

[Service(ServiceLifetime.Singleton)]
public class RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger) : IRedisCacheService
{
    private readonly IDistributedCache _distributedCache = distributedCache;
    private readonly ILogger<RedisCacheService> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        try
        {
            var bytes = await _distributedCache.GetAsync(key, token);
            return Deserialize<T>(key, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCacheService.GetAsync failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken token = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _distributedCache.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCacheService.SetAsync failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RedisCacheService.RemoveAsync failed for key {Key}", key);
        }
    }

    // ---- Shared helper ----

    private T? Deserialize<T>(string key, byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "RedisCacheService: failed to deserialize cached value for key {Key}. Treating as cache miss.", key);
            return default;
        }
    }
}