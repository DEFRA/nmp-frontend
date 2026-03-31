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
    public class RecommendationService(ILogger<RecommendationService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IRecommendationService
    {
        private readonly ILogger<RecommendationService> _logger = logger;

        public async Task<(Recommendation?, Error?)> FetchRecommendationByManagementPeriodId(int managementPeriodID)
        {
            Recommendation? recommendation = null;
            Error? error = null;
            try
            {
                string url = string.Format(ApiurlHelper.FetchRecommendationByManagementPeriodIdAsyncAPI, managementPeriodID);
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(url);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    recommendation = responseWrapper?.Data?.record?.ToObject<Recommendation>();                   
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
            return (recommendation, error);
        }
    }
}
