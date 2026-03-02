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
namespace NMP.Services;

    [Service(ServiceLifetime.Scoped)]
    public class FarmsNvzService(ILogger<FarmsNvzService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService),IFarmsNvzService
    {
        private readonly ILogger<FarmsNvzService> _logger = logger;

        public async Task<(List<FarmsNvz>, Error?)> FetchFarmNVZByID(int farmId)
        {
            List<FarmsNvz> farmNVZList = new List<FarmsNvz>();
            Error? error = null;
            try
            {
                string url = string.Format(ApiurlHelper.FetchFarmsNVZByFarmIdAsyncAPI, farmId);
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(url);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode && responseWrapper != null && responseWrapper.Data != null)
                {
                    List<FarmsNvz>? FarmsNVZ = responseWrapper?.Data?.FarmsNVZ?.ToObject<List<FarmsNvz>>();
                    if (FarmsNVZ != null && FarmsNVZ.Count > 0)
                    {
                        farmNVZList.AddRange(FarmsNVZ);
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
            return (farmNVZList, error);
        }
    }

