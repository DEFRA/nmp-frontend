using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Core.Interfaces;
using System;
using NMP.Core.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NMP.Services;

    [Service(ServiceLifetime.Scoped)]
    public class FarmAverageYieldService(ILogger<FarmAverageYieldService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IFarmAverageYieldService
    {
        private readonly ILogger<FarmAverageYieldService> _logger = logger;

        public async Task<(List<FarmAverageYields>?, Error?)> FetchFarmAverageYieldByFarmIdAndHarvestYear(int farmId, int harvestYear)
        {
            List<FarmAverageYields>? farmAverageYieldList = null;
            Error? error = null;
            try
            {
                string url = string.Format(ApiurlHelper.FetchFarmAverageYieldByFarmIdAndHarvestYearAsyncAPI, farmId, harvestYear);
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(url);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<FarmAverageYields>? farmAverageYields = responseWrapper?.Data?.FarmAverageYields?.ToObject<List<FarmAverageYields>>();
                    if (farmAverageYields != null && farmAverageYields.Count > 0)
                    {
                        farmAverageYieldList = farmAverageYields;
                    }
                }
                else
                {
                    error = _logger.ExtractError(responseWrapper, error);
                }
            }
            catch (HttpRequestException hre)
            {
                error ??= new Error();
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre, hre.Message);
            }
            catch (Exception ex)
            {
                error ??= new Error();
                error.Message = ex.Message;
                _logger.LogError(ex, ex.Message);
            }
            return (farmAverageYieldList, error);
        }
    }

