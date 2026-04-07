using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
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
    public class ScotlandNMaxValueService(ILogger<ScotlandNMaxValueService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IScotlandNMaxValueService
    {
        private readonly ILogger<ScotlandNMaxValueService> _logger = logger;
        public async Task<(List<ScotlandNMaxValue>?, Error?)> FetchAllScotlandNMaxValue()
        {
            _logger.LogTrace("Fetch all ScotlandNMaxValue");
            List<ScotlandNMaxValue> scotlandNMaxValueList = new List<ScotlandNMaxValue>();
            Error? error = null;
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(ApiurlHelper.FetchAllScotlandNMaxValuesAsyncAPI);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<ScotlandNMaxValue>? scotlandNMaxValues = responseWrapper?.Data?.records.ToObject<List<ScotlandNMaxValue>>();
                    if (scotlandNMaxValues != null && scotlandNMaxValues.Count > 0)
                    {
                        scotlandNMaxValueList.AddRange(scotlandNMaxValues);
                    }
                }
                else
                {
                    error = _logger.ExtractError(responseWrapper, error);
                }
            }
            catch (HttpRequestException hre)
            {
                error = new Error();
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre, hre.Message);
            }
            catch (Exception ex)
            {
                error = new Error();
                error.Message = ex.Message;
                _logger.LogError(ex, ex.Message);
            }

            return (scotlandNMaxValueList, error);
        }
    }
}
