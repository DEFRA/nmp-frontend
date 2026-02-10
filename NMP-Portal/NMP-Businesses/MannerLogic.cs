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
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class MannerLogic(ILogger<MannerLogic> logger, IMannerService mannerService, IFarmLogic farmLogic, IHttpContextAccessor httpContextAccessor) : IMannerLogic
{
    private readonly ILogger<MannerLogic> _logger = logger;
    private readonly IMannerService _mannerService = mannerService;
    private readonly IFarmLogic _farmLogic = farmLogic;
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

    public MannerEstimationStep1ViewModel SetMannerEstimationStep1(MannerEstimationStep1ViewModel mannerEstimationStep1)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep1 = mannerEstimationStep1;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep1();
    }


    public MannerEstimationStep1ViewModel GetMannerEstimationStep1()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep1.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep1;
    }

    public MannerEstimationStep2ViewModel GetMannerEstimationStep2()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep2.FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
        return mannerEstimationViewModel.MannerEstimationStep2;
    }
    public MannerEstimationStep2ViewModel SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2 = mannerEstimationStep2;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep2();
    }

    public MannerEstimationStep3ViewModel GetMannerEstimationStep3()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep3.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep3.FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
        return mannerEstimationViewModel.MannerEstimationStep3;
    }
    public async Task<MannerEstimationStep3ViewModel> SetMannerEstimationStep3(MannerEstimationStep3ViewModel mannerEstimationStep3)
    {
        MannerEstimationStep3ViewModel previousMannerEstimationStep3ViewModel = GetMannerEstimationStep3();
        string? oldPostcode = previousMannerEstimationStep3ViewModel?.Postcode?.Trim();
        string? newPostcode = mannerEstimationStep3.Postcode?.Trim();
        if (!string.IsNullOrWhiteSpace(oldPostcode) && !string.IsNullOrWhiteSpace(newPostcode))
        {
            mannerEstimationStep3.IsPostCodeChange =
                !string.Equals(oldPostcode, newPostcode, StringComparison.OrdinalIgnoreCase);
            if (mannerEstimationStep3.IsPostCodeChange)
            {
                MannerEstimationStep4ViewModel mannerEstimationStep4ViewModel = await GetMannerEstimationStep4();
                mannerEstimationStep4ViewModel.AverageAnnualRainfall = 0;
                await SetMannerEstimationStep4(mannerEstimationStep4ViewModel);
            }
        }
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

    private void SetMannerEstimationToSession(MannerEstimationViewModel mannerEstimationViewModel)
    {
        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_mannerEstimationSessionName, mannerEstimationViewModel);
    }



    public async Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep4.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep4.Postcode = mannerEstimationViewModel.MannerEstimationStep3.Postcode;
        if (mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall == 0)
        {
            mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall = await FetchAverageAnnualRainfall(mannerEstimationViewModel.MannerEstimationStep4);
            SetMannerEstimationToSession(mannerEstimationViewModel);
        }
        return mannerEstimationViewModel.MannerEstimationStep4;
    }
    public async Task<MannerEstimationStep4ViewModel> SetMannerEstimationStep4(MannerEstimationStep4ViewModel mannerEstimationStep4)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep4 = mannerEstimationStep4;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep4();
    }

    private async Task<int> FetchAverageAnnualRainfall(MannerEstimationStep4ViewModel mannerEstimationStep4)
    {
        string firstHalfPostcode = Functions.ExtractFirstHalfPostcode(mannerEstimationStep4.Postcode);

        decimal rainfall = await _farmLogic.FetchRainfallAverageAsync(firstHalfPostcode);
        return (int)Math.Round(rainfall);
    }

    public MannerEstimationStep5ViewModel GetMannerEstimationStep5()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep5.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep5;
    }
    public MannerEstimationStep5ViewModel SetMannerEstimationStep5(MannerEstimationStep5ViewModel mannerEstimationStep5)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep5 = mannerEstimationStep5;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep5();
    }

    public MannerEstimationStep6ViewModel GetMannerEstimationStep6()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep6.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep6.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        return mannerEstimationViewModel.MannerEstimationStep6;
    }

    public MannerEstimationStep6ViewModel SetMannerEstimationStep6(MannerEstimationStep6ViewModel mannerEstimationStep6)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep6 = mannerEstimationStep6;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep6();
    }
}
