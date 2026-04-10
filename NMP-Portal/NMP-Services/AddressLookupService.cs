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
public class AddressLookupService(ILogger<AddressLookupService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IAddressLookupService
{
    private readonly ILogger<AddressLookupService> _logger = logger;

    public async Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
    {
        List<AddressLookupResponse> addresses = new();
        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var requisteUrl = string.Format(ApiurlHelper.AddressLookupAPI, HttpUtility.UrlEncode(postcode), HttpUtility.UrlEncode(offset.ToString()));
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

        return addresses;
    }
}
