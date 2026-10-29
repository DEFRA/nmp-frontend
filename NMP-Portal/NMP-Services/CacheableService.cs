using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NMP.Services
{
    public abstract class CacheableService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : Service(httpContextAccessor, clientFactory, tokenRefreshService)
    {
        private readonly IRedisCacheService _redisCacheService = redisCacheService;
        private readonly IMemoryCacheService _memoryCacheService = memoryCacheService;
        protected static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);
        // Tiered read: Redis first, then in-memory.
        protected async Task<T?> GetFromCacheAsync<T>(string cacheKey) where T : class
        {
            var redisValue = await _redisCacheService.GetAsync<T>(cacheKey);
            if (redisValue != null)
            {
                Console.WriteLine(string.Format(Resource.lblCacheRedis, cacheKey));
                return redisValue;
            }
            var memoryValue = _memoryCacheService.Get<T>(cacheKey);
            if (memoryValue != null)
            {
                Console.WriteLine(string.Format(Resource.lblCacheMemory, cacheKey));
                return memoryValue;
            }
            return null;
        }
        // Writes to both tiers so they stay in sync.
        protected async Task SetCacheAsync<T>(string cacheKey, T value, TimeSpan? cacheDuration = null) where T : class
        {
            var duration = cacheDuration ?? DefaultCacheDuration;
            try
            {
                await _redisCacheService.SetAsync(cacheKey, value, duration);
            }
            catch (Exception)
            {
                _memoryCacheService.Set(cacheKey, value, duration);
            }
            Console.WriteLine(string.Format(Resource.lblCacheSet, cacheKey));
        }
        protected async Task RemoveCacheAsync(string cacheKey)
        {
            await _redisCacheService.RemoveAsync(cacheKey);
            _memoryCacheService.Remove(cacheKey);
        }
    }
}