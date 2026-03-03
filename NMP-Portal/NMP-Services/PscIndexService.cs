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
namespace NMP.Services;

    [Service(ServiceLifetime.Scoped)]
    public class PscIndexService(ILogger<PscIndexService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService),IPscIndexService
    {
        private readonly ILogger<PscIndexService> _logger = logger;

        public async Task<List<CommonResponse>> FetchPscIndex()
    {
            List<CommonResponse> pscIndexList = [];
            try
            {
                HttpClient httpClient = await GetNMPAPIClient();
                var response = await httpClient.GetAsync(ApiurlHelper.FetchPscIndexesAsyncAPI);

                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var pscIndexes = responseWrapper?.Data?.records.ToObject<List<CommonResponse>>();
                        pscIndexList.AddRange(pscIndexes);
                    }
                }
                else
                {
                    _logger.ExtractError(responseWrapper, null);
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, hre.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return pscIndexList;
        }

    }

