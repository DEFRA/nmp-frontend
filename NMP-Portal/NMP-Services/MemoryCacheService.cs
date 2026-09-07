using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NMP.Core.Attributes;

namespace NMP.Services;

    [Service(ServiceLifetime.Singleton)]
    public class MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger) : IMemoryCacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly ILogger<MemoryCacheService> _logger = logger;

        public T? Get<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    return value;
                }
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MemoryCacheService.Get failed for key {Key}", key);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                _memoryCache.Set(key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MemoryCacheService.Set failed for key {Key}", key);
                // Swallow — failing to cache shouldn't fail the calling operation
            }
        }

        public void Remove(string key)
        {
            try
            {
                _memoryCache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MemoryCacheService.Remove failed for key {Key}", key);
            }
        }
    }
