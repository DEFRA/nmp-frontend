using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IOrganicManureService
    {
        Task<(List<ManureCropTypeResponse>,Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId,int harvestYear);
        Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId);
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId);
        Task<(List<CommonResponse>, Error)> FetchManureGroupList();
        Task<(List<ManureType>, Error)> FetchManureTypeList(int manureGroupId, int countryId);
        Task<(CommonResponse, Error)> FetchManureGroupById(int manureGroupId);

        Task<(ManureType, Error)> FetchManureTypeByManureTypeId(int manureTypeId);

        Task<(List<ApplicationMethodResponse>, Error)> FetchApplicationMethodList(int fieldType,bool isLiquid);

        Task<(List<IncorporationMethodResponse>, Error)> FetchIncorporationMethodsByApplicationId(int appId,string? applicableFor);
        Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor);
               
        Task<(string, Error)> FetchApplicationMethodById(int Id);
        Task<(string, Error)> FetchIncorporationMethodById(int Id);
        Task<(string, Error)> FetchIncorporationDelayById(int Id);
        Task<(bool, Error)> AddOrganicManuresAsync(string organicManureData);

        Task<(RainTypeResponse, Error)> FetchRainTypeDefault();
        Task<int> FetchRainfallByPostcodeAndDateRange(string jsonString);

        Task<(WindspeedResponse, Error)> FetchWindspeedDataDefault();
        Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate);

        Task<(List<RainTypeResponse>, Error)> FetchRainTypeList();
        Task<(List<WindspeedResponse>, Error)> FetchWindspeedList();
        Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList();
        Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId);
        Task<(decimal, Error)> FetchTotalNBasedOnManIdAndAppDate(int managementId, DateTime startDate, DateTime endDate, bool confirm);
        Task<(CropTypeResponse, Error)> FetchCropTypeByFieldIdAndHarvestYear(int fieldId, int year,bool confirm);
        Task<(CropTypeLinkingResponse, Error)> FetchCropTypeLinkingByCropTypeId(int cropTypeId);
        Task<(List<int>, Error)> FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(int fieldId, int year,bool confirm);
        Task<(decimal, Error)> FetchTotalNBasedOnManIdFromOrgManureAndFertiliser(int managementId, bool confirm);
        Task<(bool, Error)> FetchOrganicManureExistanceByDateRange(string dateFrom, string dateTo, bool isConfirm);
        Task<(NitrogenUptakeResponse, Error)> FetchAutumnCropNitrogenUptake(string jsonString);
        Task<(RainTypeResponse, Error)> FetchRainTypeById(int rainTypeId);
        Task<(WindspeedResponse, Error)> FetchWindspeedById(int windspeedId);
        Task<(MoistureTypeResponse, Error)> FetchMoisterTypeById(int moisterTypeId);
    }
}
