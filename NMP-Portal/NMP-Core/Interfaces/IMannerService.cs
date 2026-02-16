using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Core.Interfaces;
public interface IMannerService
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    Task<decimal> FetchRainfallAverageAsync(string firstHalfPostcode);
    Task<List<SoilTypesResponse>> FetchSoilTypes();
    Task<Country?> FetchCountryById(int id);
    Task<(List<CommonResponse>, Error?)> FetchManureGroupList();
    Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId);
    Task<(CommonResponse?, Error?)> FetchManureGroupById(int manureGroupId);

    Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId);

    Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid);

    Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor);
    Task<(List<IncorprationDelaysResponse>?, Error?)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor);

    Task<(string?, Error?)> FetchApplicationMethodById(int Id);
    Task<(string?, Error?)> FetchIncorporationMethodById(int Id);
    Task<(string?, Error?)> FetchIncorporationDelayById(int Id);
}
