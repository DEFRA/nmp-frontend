using Newtonsoft.Json.Linq;
using NMP.Portal.Models;
using System.Reflection;
using System;
using NMP.Portal.Helpers;
using Newtonsoft.Json;
using System.Net;
using NMP.Portal.ServiceResponses;
using Microsoft.Extensions.Logging;

namespace NMP.Portal.Services
{
    public class AddressLookupService : Service, IAddressLookupService
    {
        private readonly ILogger<AddressLookupService> _logger;
        public AddressLookupService(ILogger<AddressLookupService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }

        public async Task<List<AddressLookupResponse>> AddressesAsync(string postcode, int offset)
        {
            List<AddressLookupResponse> addresses = new List<AddressLookupResponse>();
            Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
            HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
            var response = await httpClient.GetAsync(string.Format(APIURLHelper.AddressLookupAPI, postcode, offset));
            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
            if (response.IsSuccessStatusCode)
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

            return addresses;
        }
    }
}
