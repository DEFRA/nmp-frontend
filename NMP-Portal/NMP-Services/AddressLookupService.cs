using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Web;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class AddressLookupService(ILogger<AddressLookupService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService, IRedisCacheService redisCacheService, IMemoryCacheService memoryCacheService) : CacheableService(httpContextAccessor, clientFactory, tokenRefreshService, redisCacheService, memoryCacheService), IAddressLookupService
{
    private readonly ILogger<AddressLookupService> _logger = logger;
    private const string _addressListKey = "AddressList";

    public async Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
    {
        string cacheKey = postcode + " " + _addressListKey;
        List<AddressLookupResponse>? addressLookupResponses = await GetAddressesFromSession(cacheKey);
        if (addressLookupResponses != null && addressLookupResponses.Count > 0)
        {
            return addressLookupResponses;
        }
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requisteUrl = string.Format(ApiurlHelper.AddressLookupAPI, HttpUtility.UrlEncode(postcode), HttpUtility.UrlEncode(offset.ToString()));
            var response = await httpClient.GetAsync(requisteUrl);

            addressLookupResponses = new List<AddressLookupResponse>();
            if (response.IsSuccessStatusCode)
            {    
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
                {
                    AddressLookupResponseWrapper? addressLookupResponseWrapper = responseWrapper?.Data?.ToObject<AddressLookupResponseWrapper>();
                    if (addressLookupResponseWrapper != null && addressLookupResponseWrapper.Results != null)
                    {
                        addressLookupResponses.AddRange(addressLookupResponseWrapper.Results);
                    }
                }
                else
                {
                    _logger.ExtractError(responseWrapper, null);
                }
            }
        }
        catch(HttpRequestException hre)
        {
            _logger.HandleHttpRequestException(hre, null);
        }
        catch (Exception ex)
        {
            _logger.HandleException(ex, null);
        }

        return addressLookupResponses;
    }
    public async Task<List<AddressLookupResponse>?> GetAddressesFromSession(string cacheKey)
    {
        var cached = await GetFromCacheAsync<List<AddressLookupResponse>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
        return null;
    }

    public async Task SetAddressesToSession(List<AddressLookupResponse> addresses, string cacheKey)
    {
        await SetCacheAsync(cacheKey, addresses, TimeSpan.FromMinutes(30));
    }
    public async Task RemoveAddressesSession(string cacheKey)
    {
        await RemoveCacheAsync(cacheKey);

    }
}
