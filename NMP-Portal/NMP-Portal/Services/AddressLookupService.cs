using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Commons.Models;
using NMP.Portal.Security;
using NMP.Commons.ServiceResponses;
using System;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;

namespace NMP.Portal.Services
{
    public class AddressLookupService : Service, IAddressLookupService
    {
        private readonly ILogger<AddressLookupService> _logger;
        public AddressLookupService(ILogger<AddressLookupService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : base(httpContextAccessor, clientFactory, tokenRefreshService)
        {
            _logger = logger;
        }

        public async Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
        {
            List<AddressLookupResponse> addresses = new List<AddressLookupResponse>();
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.AddressLookupAPI, postcode, offset));
            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (responseWrapper != null && responseWrapper.Data != null && responseWrapper.Data.GetType().Name.ToLower() != "string")
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        AddressLookupResponseWrapper addressLookupResponseWrapper = responseWrapper.Data.ToObject<AddressLookupResponseWrapper>();
                        addresses.AddRange(addressLookupResponseWrapper.Results);
                    }
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        Error error = responseWrapper.Error.ToObject<Error>();
                        _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                    }
                }
            }

            return addresses;
        }
    }
}
