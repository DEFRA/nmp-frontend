using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Web;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class AddressLookupService(ILogger<AddressLookupService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IAddressLookupService
{
    private readonly ILogger<AddressLookupService> _logger = logger;

    public async Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
    {
        List<AddressLookupResponse> addresses = new();
        HttpClient httpClient = await GetNMPAPIClient();
        var requisteUrl = string.Format(APIURLHelper.AddressLookupAPI, HttpUtility.UrlEncode(postcode), HttpUtility.UrlEncode(offset.ToString()));
        var response = await httpClient.GetAsync(requisteUrl);
        response.EnsureSuccessStatusCode();
        if (response.IsSuccessStatusCode)
        {            
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (responseWrapper != null && responseWrapper.Data != null && responseWrapper?.Data?.GetType().Name.ToLower() != "string")
            {
                AddressLookupResponseWrapper? addressLookupResponseWrapper = responseWrapper?.Data?.ToObject<AddressLookupResponseWrapper>();
                if (addressLookupResponseWrapper != null && addressLookupResponseWrapper.Results != null)
                {
                    addresses.AddRange(addressLookupResponseWrapper.Results);
                }
            }
            else
            {
                if (responseWrapper != null && responseWrapper.Error != null)
                {
                    Error? error = responseWrapper?.Error?.ToObject<Error>();
                    _logger.LogError("{Code} : {Message} : {Stack} : {Path}", error?.Code, error?.Message, error?.Stack, error?.Path);
                }
            }
        }

        return addresses;
    }
}
