using Microsoft.AspNetCore.Mvc.ModelBinding;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface IMannerLogic
{
    Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId);
    Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId);
    
    Task<decimal> FetchRainfallAverageAsync(string postcode);
    Task<List<SoilTypesResponse>> FetchSoilTypes();
    Task<Country?> FetchCountryById(int id);
    Task<List<SoilTypesResponse>> FetchSoilTypesByRB209CountryId(int rb209CountryId);
    Task<(List<CommonResponse>, Error?)> FetchManureGroupList();
    Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId);
    Task<(CommonResponse, Error?)> FetchManureGroupById(int manureGroupId);

    Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId);

    Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid);

    Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor);
    Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor);

    Task<(string, Error)> FetchApplicationMethodById(int Id);
    Task<(string, Error)> FetchIncorporationMethodById(int Id);
    Task<(string, Error)> FetchIncorporationDelayById(int Id);
    Task<(List<CommonResponse>?, Error?)> FetchTopsoilList();
    Task<(List<CommonResponse>?, Error?)> FetchSubsoilList();
    Task<(ManureNutrientResponse?, Error?)> CalculateDefaultNutrientValueBasedOnDryMatter(ManureNutrientResponse manureNutrientResponse);


}
