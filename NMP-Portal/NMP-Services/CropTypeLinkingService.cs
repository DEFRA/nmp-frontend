using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Services
{
    [Service(ServiceLifetime.Scoped)]
    public class CropTypeLinkingService(ILogger<CropTypeLinkingService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : CacheableService(httpContextAccessor, clientFactory, tokenRefreshService, redisCacheService, memoryCacheService), ICropTypeLinkingService
    {
        private readonly ILogger<CropTypeLinkingService> _logger = logger;
        public async Task<(List<CropTypeLinkingResponse>?, Error?)> FetchCropTypeLinkingAsync()
        {
            string cacheKey = "FetchCropTypeLinkingAsync";
            var cached = await GetFromCacheAsync<List<CropTypeLinkingResponse>>(cacheKey);
            if (cached != null)
            {
                return (cached, null);
            }

            Error? error = null;
            List<CropTypeLinkingResponse>? cropTypeLinkingResponse = new List<CropTypeLinkingResponse>();
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(ApiurlHelper.FetchCropTypeLinkingsAPI);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    cropTypeLinkingResponse = responseWrapper?.Data?.CropTypeLinking.records.ToObject<List<CropTypeLinkingResponse>>();
                    await SetCacheAsync(cacheKey, cropTypeLinkingResponse);

                }
                else
                {
                    error = _logger.ExtractError(responseWrapper, error);
                }
            }
            catch (HttpRequestException hre)
            {
                error = _logger.HandleHttpRequestException(hre, error);
            }
            catch (Exception ex)
            {
                error = _logger.HandleException(ex, error);
            }
            return (cropTypeLinkingResponse, error);
        }
        
    }
}
