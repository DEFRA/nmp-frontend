using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken token = default);
        Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken token = default);
        Task RemoveAsync(string key, CancellationToken token = default);
    }
}
