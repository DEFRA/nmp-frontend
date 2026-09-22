using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class SoilLogic(ILogger<SoilLogic> logger, IRb209Service rb209Service) : ISoilLogic
{
    private readonly ILogger<SoilLogic> _logger = logger;
    private readonly IRb209Service _rb209Service = rb209Service;
    public async Task<(string, Error)> FetchSoilNutrientIndex(int nutrientId, decimal? nutrientValue, int methodologyId, int countryId)
    {
        _logger.LogTrace("Fetching soil nutrient index");
        return await _rb209Service.FetchSoilNutrientIndex(nutrientId, nutrientValue, methodologyId,countryId);
    }

    public async Task<string> FetchSoilTypeById(int soilTypeId)
    {
        _logger.LogTrace("Fetching soil type by Id");
        return await _rb209Service.FetchSoilTypeById(soilTypeId);
    }

    public async Task<(List<SoilMethologiesResponse>?, Error?)> FetchSoilMethodologies(int nutrientId, int countryId)
    {
        _logger.LogTrace("Fetch all soil analysis method");
        return await _rb209Service.FetchSoilMethodologies(nutrientId,countryId);
    }
    public async Task<(SoilMethologiesResponse?, Error?)> FetchSoilMethodologyNameByNutrientIdAndMethodologyId(int nutrientId, int methodologyId)
    {
        _logger.LogTrace("Fetch soil methodology name by NutrientId:{NutrientId} and MethodologyId:{MethodologyId}",nutrientId,methodologyId);
        return await _rb209Service.FetchSoilMethodologyNameByNutrientIdAndMethodologyId(nutrientId,methodologyId);
    }

    public async Task<(List<SoilNutrientStatusResponse>?, Error?)> FetchSoilNutrientStatusList(int methodologyId)
    {
        _logger.LogTrace("Fetch Soil nutrient status list by methodologyId");
        return await _rb209Service.FetchSoilNutrientStatusList(methodologyId);
    }
}
