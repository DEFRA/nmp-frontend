﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public class CropService : Service, ICropService
    {
        private readonly ILogger<CropService> _logger;
        public CropService(ILogger<CropService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base(httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }
        public async Task<List<PotatoVarietyResponse>> FetchPotatoVarieties()
        {
            List<PotatoVarietyResponse> potatoVarieties = new List<PotatoVarietyResponse>();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(APIURLHelper.FetchPotatoVarietiesAsyncAPI);
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var potatoVarietyList = responseWrapper.Data.ToObject<List<PotatoVarietyResponse>>();
                        potatoVarieties.AddRange(potatoVarietyList);
                    }
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = responseWrapper.Error.ToObject<Error>();
                        _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }
            return potatoVarieties;
        }
        public async Task<CropTypeResponse> FetchCropTypeByGroupId(int cropGroupId)
        {
            CropTypeResponse cropTypeResponse = new CropTypeResponse();
            Error error = new Error();
            try
            {
                Token? token = _httpContextAccessor.HttpContext?.Session.GetObjectFromJson<Token>("token");
                HttpClient httpClient = this._clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCropTypesAsyncAPI, cropGroupId));
                string result = await response.Content.ReadAsStringAsync();
                ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
                if (response.IsSuccessStatusCode)
                {
                    if (responseWrapper != null && responseWrapper.Data != null)
                    {
                        var soiltypeslist = responseWrapper.Data.ToObject<List<CropTypeResponse>>();
                        var cropTypeResponses = responseWrapper.Data.ToObject<List<CropTypeResponse>>();
                        if (cropTypeResponses != null)
                        {
                            cropTypeResponse = cropTypeResponses[0];
                        }
                    }
                }
                else
                {
                    if (responseWrapper != null && responseWrapper.Error != null)
                    {
                        error = responseWrapper.Error.ToObject<Error>();
                        _logger.LogError($"{error.Code} : {error.Message} : {error.Stack} : {error.Path}");
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                error.Message = Resource.MsgServiceNotAvailable;
                _logger.LogError(hre.Message);
                throw new Exception(error.Message, hre);
            }
            catch (Exception ex)
            {
                error.Message = ex.Message;
                _logger.LogError(ex.Message);
                throw new Exception(error.Message, ex);
            }
            return cropTypeResponse;
        }
    }
}
