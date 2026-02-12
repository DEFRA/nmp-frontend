using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Services;

[Service(ServiceLifetime.Scoped)]
public class MannerService(ILogger<MannerService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefreshService) : Service(httpContextAccessor, clientFactory, tokenRefreshService), IMannerService
{
    private readonly ILogger<MannerService> _logger = logger;
    Dictionary<int, int> cropTypeToCategoryId = new Dictionary<int, int>
    {
        { 0, 2 },
        { 1, 2 },
        { 2, 6 },
        { 3, 6 },
        { 4, 2 },
        { 5, 6 },
        { 6, 2 },
        { 7, 6 },
        { 8, 2 },
        { 9, 6 },
        { 171, 6 },
        { 172, 6 },
        { 173, 6 },
        { 174, 6 },
        { 20, 4 },
        { 21, 6 },
        { 22, 9 },
        { 23, 9 },
        { 24, 9 },
        { 25, 9 },
        { 26, 8 },
        { 28, 9 },
        { 175, 9 },
        { 176, 9 },
        { 187, 9 },
        { 27, 9 },
        { 40, 9 },
        { 41, 9 },
        { 43, 9 },
        { 44, 9 },
        { 45, 9 },
        { 50, 6 },
        { 51, 6 },
        { 52, 2 },
        { 53, 2 },
        { 54, 6 },
        { 55, 6 },
        { 56, 6 },
        { 57, 2 },
        { 58, 2 },
        { 59, 2 },
        { 188, 9 },
        { 189, 9 },
        { 191, 9 },
        { 194, 9 },
        { 195, 9 },
        { 60, 9 },
        { 61, 9 },
        { 62, 9 },
        { 63, 9 },
        { 64, 9 },
        { 65, 9 },
        { 66, 9 },
        { 67, 9 },
        { 68, 9 },
        { 69, 9 },
        { 70, 9 },
        { 71, 9 },
        { 72, 9 },
        { 73, 9 },
        { 74, 9 },
        { 75, 9 },
        { 77, 9 },
        { 78, 9 },
        { 79, 9 },
        { 181, 9 },
        { 90, 8 },
        { 91, 9 },
        { 92, 9 },
        { 93, 9 },
        { 94, 9 },
        { 182, 9 },
        { 110, 9 },
        { 111, 9 },
        { 112, 9 },
        { 113, 9 },
        { 114, 9 },
        { 115, 9 },
        { 116, 9 },
        { 117, 9 },
        { 118, 9 },
        { 119, 9 },
        { 120, 9 },
        { 121, 9 },
        { 122, 9 },
        { 123, 9 },
        { 124, 9 },
        { 125, 9 },
        { 177, 9 },
        { 178, 9 },
        { 140, 1 },
        { 160, 7 },
        { 161, 7 },
        { 162, 7 },
        { 163, 7 },
        { 170, 9 },
        { 184, 9 },
        { 185, 9 },
        { 192, 9 },
        { 193, 9 },
        { 76, 9 },
        { 179, 9 },
        { 180, 9 }
    };
    public async Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId)
    {
        if (cropTypeToCategoryId.TryGetValue(cropTypeId, out int categoryId))
        {
            return categoryId;
        }
        else
        {
            return 0;
        }
    }

    public async Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId)
    {
        _logger.LogTrace("MannerService: FetchCropNUptakeDefaultAsync called for CropCategoryId: {CropCategoryId}", cropCategoryId);
        int cropUptakeFactor;

        switch (cropCategoryId)
        {
            case (int)NMP.Commons.Enums.CropCategory.Grass:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Grass;
                break;
            case (int)NMP.Commons.Enums.CropCategory.EarlySownWinterCereal:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.EarlySownWinterCereal;
                break;
            case (int)NMP.Commons.Enums.CropCategory.LateSownWinterCereal:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.LateSownWinterCereal;
                break;
            case (int)NMP.Commons.Enums.CropCategory.EarlyStablishedWinterOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.EarlyStablishedWinterOilseedRape;
                break;
            case (int)NMP.Commons.Enums.CropCategory.LateStablishedWinterOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.LateStablishedWinterOilseedRape;
                break;
            case (int)NMP.Commons.Enums.CropCategory.Other:
            case (int)NMP.Commons.Enums.CropCategory.Potatoes:
            case (int)NMP.Commons.Enums.CropCategory.Sugerbeet:
            case (int)NMP.Commons.Enums.CropCategory.SpringCerealOilseedRape:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Other;
                break;
            default:
                cropUptakeFactor = (int)NMP.Commons.Enums.CropUptakeFactor.Other;
                break;
        }

        return cropUptakeFactor;
    }

    public async Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode)
    {
        decimal rainfallAverage = 0;
        string url = string.Format(APIURLHelper.FetchMannerRainfallAverageAsyncAPI, firstHalfPostcode);
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            rainfallAverage = responseWrapper?.Data?.avarageAnnualRainfall?.value ?? 0;
        }

        return rainfallAverage;
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        List<SoilTypesResponse> soilTypes = new List<SoilTypesResponse>();
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(APIURLHelper.FetchSoilTypesAsyncAPI);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(result);
        if (response.IsSuccessStatusCode)
        {
            if (responseWrapper != null && responseWrapper.Data != null)
            {
                var soiltypeslist = responseWrapper.Data.ToObject<List<SoilTypesResponse>>();
                soilTypes.AddRange(soiltypeslist);
            }
        }
        return soilTypes;
    }

    public async Task<Country?> FetchCountryById(int id)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(string.Format(APIURLHelper.FetchCountryByIdAsyncAPI, id));
        string result = await response.Content.ReadAsStringAsync();
        ResponseWrapper? responseWrapper =
            JsonConvert.DeserializeObject<ResponseWrapper>(result);

        if (responseWrapper?.Data?.records != null)
        {
            return responseWrapper.Data.records.ToObject<Country>();
        }

        return null;
    }


}
