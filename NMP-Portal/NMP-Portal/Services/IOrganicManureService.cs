using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IOrganicManureService
    {
        Task<(List<OrganicManureCropTypeResponse>,Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId,int harvestYear);
        Task<(List<OrganicManureFieldResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropTypeId(int harvestYear, int farmId, string? cropTypeId);
        Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropTypeId(int harvestYear, string fieldIds, string? cropTypeId);
        Task<(List<CommonResponse>, Error)> FetchManureGroupList();
        Task<(List<ManureType>, Error)> FetchManureTypeList(int manureGroupId, int countryId);
        Task<(CommonResponse, Error)> FetchManureGroupById(int manureGroupId);

        Task<(ManureType, Error)> FetchManureTypeByManureTypeId(int manureTypeId);

        Task<(List<ApplicationMethodResponse>, Error)> FetchApplicationMethodList(int fieldType,string applicableFor);

        Task<(List<IncorporationMethodResponse>, Error)> FetchIncorporationMethodsByApplicationId(int fieldType,string applicableFor,int appId);
        Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor);
               
        Task<(string, Error)> FetchApplicationMethodById(int Id);
        Task<(string, Error)> FetchIncorporationMethodById(int Id);
        Task<(string, Error)> FetchIncorporationDelayById(int Id);
        Task<(bool, Error)> AddOrganicManuresAsync(string organicManureData);

        Task<(RainTypeResponse, Error)> FetchRainTypeDefault();
        Task<int> FetchRainfallByPostcodeAndDateRange(string postCode, string applicationDate, string soilDrainageDate);

        Task<(WindspeedResponse, Error)> FetchWindspeedDataDefault();
        Task<(MoistureTypeResponse, Error)> FetchMoisterTypeDefaultByApplicationDate(string applicationDate);

        Task<(List<RainTypeResponse>, Error)> FetchRainTypeList();
        Task<(List<WindspeedResponse>, Error)> FetchWindspeedList();
        Task<(List<MoistureTypeResponse>, Error)> FetchMoisterTypeList();

    }
}
