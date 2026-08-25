using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Application;
using NMP.Commons.Enums;
using NMP.Commons.Helpers;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Reflection;
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class MannerLogic(ILogger<MannerLogic> logger, IMannerService mannerService) : IMannerLogic
{
    private readonly ILogger<MannerLogic> _logger = logger;
    private readonly IMannerService _mannerService = mannerService;
    public async Task<int> FetchCategoryIdByCropTypeIdAsync(int cropTypeId)
    {
        _logger.LogTrace("Fetching category Id by crop type Id");
        return await _mannerService.FetchCategoryIdByCropTypeIdAsync(cropTypeId);
    }

    public async Task<int> FetchCropNUptakeDefaultAsync(int cropCategoryId)
    {
        _logger.LogTrace("Fetching crop N uptake default");
        return await _mannerService.FetchCropNUptakeDefaultAsync(cropCategoryId);
    }



    public async Task<decimal> FetchRainfallAverageAsync(string postcode)
    {
        _logger.LogTrace("Fetching rainfall average for Postcode: {Postcode}", postcode);
        return await _mannerService.FetchRainfallAverageAsync(postcode);
    }
    public async Task<List<SoilTypesResponse>> FetchSoilTypes()
    {
        _logger.LogTrace("Fetching soil types");
        return await _mannerService.FetchSoilTypes();
    }

    public async Task<List<SoilTypesResponse>> FetchSoilTypesByRB209CountryId(int rb209CountryId)
    {
        _logger.LogTrace("Fetching soil types by RB209 Country Id");
        List<SoilTypesResponse> soilTypes = await FetchSoilTypes();
        return [.. soilTypes.Where(x => x.CountryId == rb209CountryId)];
    }


    public async Task<Country?> FetchCountryById(int id)
    {
        _logger.LogTrace("Fetching country by id");
        return await _mannerService.FetchCountryById(id);
    }
    public async Task<(CommonResponse, Error?)> FetchManureGroupById(int manureGroupId)
    {
        _logger.LogTrace("MannerLogic : FetchManureGroupById() called");
        return await _mannerService.FetchManureGroupById(manureGroupId);
    }

    public async Task<(List<CommonResponse>, Error?)> FetchManureGroupList()
    {
        _logger.LogTrace("MannerLogic : FetchManureGroupList() called");
        return await _mannerService.FetchManureGroupList();
    }

    public async Task<(ManureType?, Error?)> FetchManureTypeByManureTypeId(int manureTypeId)
    {
        _logger.LogTrace("MannerLogic : FetchManureTypeByManureTypeId() called");
        return await _mannerService.FetchManureTypeByManureTypeId(manureTypeId);
    }

    public async Task<(List<ManureType>, Error?)> FetchManureTypeList(int manureGroupId, int countryId)
    {
        _logger.LogTrace("MannerLogic : FetchManureTypeList() called");
        (List<ManureType> manures, Error? error) = await _mannerService.FetchManureTypeList(manureGroupId, countryId);
        return (manures.OrderBy(m => m.SortOrder).ToList(), error);
    }
    public async Task<(string, Error)> FetchApplicationMethodById(int Id)
    {
        _logger.LogTrace("MannerLogic : FetchApplicationMethodById() called");
        return await _mannerService.FetchApplicationMethodById(Id);
    }

    public async Task<(List<ApplicationMethodResponse>, Error?)> FetchApplicationMethodList(int fieldType, bool isLiquid)
    {
        _logger.LogTrace("MannerLogic : FetchApplicationMethodList() called");
        return await _mannerService.FetchApplicationMethodList(fieldType, isLiquid);
    }

    public async Task<(string, Error)> FetchIncorporationDelayById(int Id)
    {
        _logger.LogTrace("MannerLogic : FetchIncorporationDelayById() called");
        return await _mannerService.FetchIncorporationDelayById(Id);
    }

    public async Task<(List<IncorprationDelaysResponse>, Error)> FetchIncorporationDelaysByMethodIdAndApplicableFor(int methodId, string applicableFor)
    {
        _logger.LogTrace("MannerLogic : FetchIncorporationDelaysByMethodIdAndApplicableFor() called");
        return await _mannerService.FetchIncorporationDelaysByMethodIdAndApplicableFor(methodId, applicableFor);
    }

    public async Task<(string, Error)> FetchIncorporationMethodById(int Id)
    {
        _logger.LogTrace("ManureLogic : FetchIncorporationMethodById() called");
        return await _mannerService.FetchIncorporationMethodById(Id);
    }

    public async Task<(List<IncorporationMethodResponse>, Error?)> FetchIncorporationMethodsByApplicationId(int appId, string? applicableFor)
    {
        _logger.LogTrace("MannerLogic : FetchIncorporationMethodsByApplicationId() called");
        return await _mannerService.FetchIncorporationMethodsByApplicationId(appId, applicableFor);
    }

    
    public async Task<(List<CommonResponse>?, Error?)> FetchTopsoilList()
    {
        _logger.LogTrace("Fetch manner top soil list");
        return await _mannerService.FetchTopsoilList();
    }
    
    public async Task<(List<CommonResponse>?, Error?)> FetchSubsoilList()
    {
        _logger.LogTrace("Fetch manner sub soil list");
        return await _mannerService.FetchSubsoilList();
    }
    public async Task<(ManureNutrientResponse?, Error?)> FetchDefaultNutrientValueBasedOnDryMatter(ManureNutrientResponse manureNutrientResponse)
    {
        _logger.LogTrace("Fetch manner default nutrient value based on dry matter");
        return await _mannerService.FetchDefaultNutrientValueBasedOnDryMatter(manureNutrientResponse);
    }
}
