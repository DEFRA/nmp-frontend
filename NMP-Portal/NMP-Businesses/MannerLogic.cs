using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Helpers;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System.Reflection;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class MannerLogic(ILogger<MannerLogic> logger, IMannerService mannerService, IHttpContextAccessor httpContextAccessor) : IMannerLogic
{
    private readonly ILogger<MannerLogic> _logger = logger;
    private readonly IMannerService _mannerService = mannerService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private const string _mannerEstimationSessionName = "MannerEstimation";
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

    public MannerEstimationStep1 SetMannerEstimationStep1(MannerEstimationStep1 mannerEstimationStep1)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep1 = mannerEstimationStep1;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep1();
    }


    public MannerEstimationStep1 GetMannerEstimationStep1()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep1.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep1;
    }

    public MannerEstimationStep2 GetMannerEstimationStep2()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer; 
        mannerEstimationViewModel.MannerEstimationStep2.FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
        return mannerEstimationViewModel.MannerEstimationStep2;
    }
    public MannerEstimationStep2 SetMannerEstimationStep2(MannerEstimationStep2 mannerEstimationStep2)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2 = mannerEstimationStep2;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep2();
    }

    public MannerEstimationStep3 GetMannerEstimationStep3()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep3.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep3.FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
        return mannerEstimationViewModel.MannerEstimationStep3;
    }
    public MannerEstimationStep3 SetMannerEstimationStep3(MannerEstimationStep3 mannerEstimationStep3)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep3 = mannerEstimationStep3;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep3();
    }

    private MannerEstimationViewModel GetMannerEstimation()
    {
        return GetMannerEstimationFromSession() ?? new MannerEstimationViewModel();
    }

    private MannerEstimationViewModel? GetMannerEstimationFromSession()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var mannerEstimation = session?.GetObjectFromJson<MannerEstimationViewModel>(_mannerEstimationSessionName);

        return mannerEstimation;
    }

    private void SetMannerEstimationToSession(MannerEstimationViewModel farm)
    {
        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_mannerEstimationSessionName, farm);
    }

}
