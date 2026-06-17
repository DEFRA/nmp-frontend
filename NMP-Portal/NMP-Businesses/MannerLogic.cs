using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
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
public class MannerLogic(ILogger<MannerLogic> logger, IMannerService mannerService, IOrganicManureLogic organicManureLogic, IHttpContextAccessor httpContextAccessor) : IMannerLogic
{
    private readonly ILogger<MannerLogic> _logger = logger;
    private readonly IMannerService _mannerService = mannerService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
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
        mannerEstimationViewModel.MannerEstimationStep1.IsFarmCopied = mannerEstimationViewModel.MannerEstimationStep15.FarmId != null;
        return mannerEstimationViewModel.MannerEstimationStep1;
    }

    public MannerEstimationStep2ViewModel GetMannerEstimationStep2()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep2.FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
        return mannerEstimationViewModel.MannerEstimationStep2;
    }
    public async Task<MannerEstimationStep2ViewModel> SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2 = mannerEstimationStep2;
        mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = await FetchFarmRB209CoutryId(mannerEstimationViewModel.MannerEstimationStep2.CountryID);
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep2();
    }

    private async Task<int?> FetchFarmRB209CoutryId(int countryId)
    {
        Country? country = await FetchCountryById(countryId);

        if (country == null)
            return null;
        return country.RB209CountryID;
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
            mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall = await FetchAnnualRainfallAverageAsync(mannerEstimationViewModel.MannerEstimationStep4);
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

    private async Task<int> FetchAnnualRainfallAverageAsync(MannerEstimationStep4ViewModel mannerEstimationStep4)
    {
        string firstHalfPostcode = Functions.ExtractFirstHalfPostcode(mannerEstimationStep4.Postcode);
        decimal rainfall = await FetchRainfallAverageAsync(firstHalfPostcode);
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

    public async Task<decimal> FetchRainfallAverageAsync(string postcode)
    {
        _logger.LogTrace("Fetching rainfall average for Postcode: {Postcode}", postcode);
        return await _mannerService.FetchRainfallAverageAsync(postcode);
    }
    public MannerEstimationStep7ViewModel GetMannerEstimationStep7()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep7.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep7.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        mannerEstimationViewModel.MannerEstimationStep7.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep23.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        return mannerEstimationViewModel.MannerEstimationStep7;
    }

    public MannerEstimationStep7ViewModel SetMannerEstimationStep7(MannerEstimationStep7ViewModel mannerEstimationStep7)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep7 = mannerEstimationStep7;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep7();
    }

    public MannerEstimationStep8ViewModel GetMannerEstimationStep8()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep8.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep8.IsFarmCopied = mannerEstimationViewModel.MannerEstimationStep15.FarmId != null;
        return mannerEstimationViewModel.MannerEstimationStep8;
    }

    public MannerEstimationStep8ViewModel SetMannerEstimationStep8(MannerEstimationStep8ViewModel mannerEstimationStep8)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep8 = mannerEstimationStep8;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep8();
    }

    public MannerEstimationStep9ViewModel GetMannerEstimationStep9()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep9.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep9.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        mannerEstimationViewModel.MannerEstimationStep9.CropGroupName = mannerEstimationViewModel.MannerEstimationStep8.CropGroupName;
        return mannerEstimationViewModel.MannerEstimationStep9;
    }

    public MannerEstimationStep9ViewModel SetMannerEstimationStep9(MannerEstimationStep9ViewModel mannerEstimationStep9)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep9 = mannerEstimationStep9;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep9();
    }

    public MannerEstimationStep10ViewModel GetMannerEstimationStep10()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep10.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep10;
    }

    public MannerEstimationStep10ViewModel SetMannerEstimationStep10(MannerEstimationStep10ViewModel mannerEstimationStep10)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep10 = mannerEstimationStep10;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep10();
    }

    public MannerEstimationStep11ViewModel GetMannerEstimationStep11()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep11.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep11.CropTypeId = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId ?? 0;
        return mannerEstimationViewModel.MannerEstimationStep11;
    }

    public MannerEstimationStep11ViewModel SetMannerEstimationStep11(MannerEstimationStep11ViewModel mannerEstimationStep11)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep11 = mannerEstimationStep11;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep11();
    }

    public MannerEstimationStep12ViewModel GetMannerEstimationStep12()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep12.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep12.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep12.ManureGroupName = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupName;
        mannerEstimationViewModel.MannerEstimationStep12.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId ?? 0;
        return mannerEstimationViewModel.MannerEstimationStep12;
    }

    public MannerEstimationStep12ViewModel SetMannerEstimationStep12(MannerEstimationStep12ViewModel mannerEstimationStep12)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep12 = mannerEstimationStep12;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep12();
    }

    public MannerEstimationStep13ViewModel GetMannerEstimationStep13()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep13.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep13.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep13.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        mannerEstimationViewModel.MannerEstimationStep13.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        return mannerEstimationViewModel.MannerEstimationStep13;
    }

    public MannerEstimationStep13ViewModel SetMannerEstimationStep13(MannerEstimationStep13ViewModel mannerEstimationStep13)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep13 = mannerEstimationStep13;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep13();
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

    public MannerEstimationStep14ViewModel SetMannerEstimationStep14(MannerEstimationStep14ViewModel mannerEstimationStep14)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep14 = mannerEstimationStep14;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep14();
    }


    public MannerEstimationStep14ViewModel GetMannerEstimationStep14()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep14.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep14;
    }
    public MannerEstimationStep15ViewModel SetMannerEstimationStep15(MannerEstimationStep15ViewModel mannerEstimationStep15)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep15 = mannerEstimationStep15;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep15();
    }


    public MannerEstimationStep15ViewModel GetMannerEstimationStep15()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep15.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep15;
    }
    public MannerEstimationStep16ViewModel SetMannerEstimationStep16(MannerEstimationStep16ViewModel mannerEstimationStep16)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep16 = mannerEstimationStep16;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep16();
    }


    public MannerEstimationStep16ViewModel GetMannerEstimationStep16()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep16.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep16.FarmId = mannerEstimationViewModel.MannerEstimationStep15.FarmId;
        return mannerEstimationViewModel.MannerEstimationStep16;
    }

    public MannerEstimationStep17ViewModel GetMannerEstimationStep17()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep17.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep17;
    }
    public MannerEstimationStep17ViewModel SetMannerEstimationStep17(MannerEstimationStep17ViewModel mannerEstimationStep17)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep17 = mannerEstimationStep17;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep17();
    }

    public MannerEstimationStep18ViewModel GetMannerEstimationStep18()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep18.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        mannerEstimationViewModel.MannerEstimationStep18.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        return mannerEstimationViewModel.MannerEstimationStep18;
    }
    public MannerEstimationStep18ViewModel SetMannerEstimationStep18(MannerEstimationStep18ViewModel mannerEstimationStep18)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep18 = mannerEstimationStep18;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep18();
    }

    public async Task<(List<CommonResponse>?, Error?)> FetchTopsoilList()
    {
        _logger.LogTrace("Fetch manner top soil list");
        return await _mannerService.FetchTopsoilList();
    }
    public MannerEstimationStep19ViewModel GetMannerEstimationStep19()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep19.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        return mannerEstimationViewModel.MannerEstimationStep19;
    }
    public MannerEstimationStep19ViewModel SetMannerEstimationStep19(MannerEstimationStep19ViewModel mannerEstimationStep19)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep19 = mannerEstimationStep19;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep19();
    }
    public async Task<(List<CommonResponse>?, Error?)> FetchSubsoilList()
    {
        _logger.LogTrace("Fetch manner sub soil list");
        return await _mannerService.FetchSubsoilList();
    }
    public MannerEstimationStep20ViewModel GetMannerEstimationStep20()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep20.CropTypeName = mannerEstimationViewModel.MannerEstimationStep9.CropTypeName;
        mannerEstimationViewModel.MannerEstimationStep20.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        return mannerEstimationViewModel.MannerEstimationStep20;
    }
    public async Task<MannerEstimationStep20ViewModel> SetMannerEstimationStep20(MannerEstimationStep20ViewModel mannerEstimationStep20)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep20 = mannerEstimationStep20;
        mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId = await BindMannerCropTypeId(mannerEstimationStep20, mannerEstimationViewModel.MannerEstimationStep9.CropTypeId.Value);
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep20();
    }
    private async Task<int?> BindMannerCropTypeId(MannerEstimationStep20ViewModel model, int cropTypeId)
    {
        (CropTypeLinkingResponse cropTypeLinkingResponse, _) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(cropTypeId);
        if (IsCropCereal(cropTypeId))
        {
            return GetWinterCerealCategory(model.SowingDate.Value, cropTypeLinkingResponse);
        }
        else if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape)
        {
            return GetWinterOilseedRapeCategory(model.SowingDate.Value, cropTypeLinkingResponse);
        }
        return null;
    }
    private static int GetWinterCerealCategory(DateTime sowingDate, CropTypeLinkingResponse cropTypeLinkingResponse)
    {
        DateTime cutoff = new DateTime(sowingDate.Year, 9, 15, 0, 0, 0, DateTimeKind.Unspecified);

        return sowingDate.Date <= cutoff
            ? cropTypeLinkingResponse.MannerCropTypeID
            : cropTypeLinkingResponse.LateSownMannerCropTypeID.Value;
    }

    private static int GetWinterOilseedRapeCategory(DateTime establishmentDate, CropTypeLinkingResponse cropTypeLinkingResponse)
    {
        DateTime cutoff = new DateTime(establishmentDate.Year, 9, 15, 0, 0, 0, DateTimeKind.Unspecified);

        return establishmentDate.Date <= cutoff
            ? cropTypeLinkingResponse.MannerCropTypeID
            : cropTypeLinkingResponse.LateSownMannerCropTypeID.Value;
    }
    private static bool IsCropCereal(int cropTypeId)
    {
        return cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterWheat ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterBarley ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOats ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterRye ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterTriticale ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WholecropWinterBarley ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.ForageWinterRye ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.ForageWinterTriticale ||
            cropTypeId == (int)NMP.Commons.Enums.CropTypes.WholecropWinterOats;
    }
    public MannerEstimationStep23ViewModel GetMannerEstimationStep23()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep23.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep23.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId;
        mannerEstimationViewModel.MannerEstimationStep23.CountryId = mannerEstimationViewModel.MannerEstimationStep2.CountryID;
        mannerEstimationViewModel.MannerEstimationStep23.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep23.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;

        return mannerEstimationViewModel.MannerEstimationStep23;
    }
    public async Task<MannerEstimationStep23ViewModel> SetMannerEstimationStep23(MannerEstimationStep23ViewModel mannerEstimationStep23)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep23 = mannerEstimationStep23;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep23();
    }
    public async Task<MannerEstimationStep24ViewModel> GetMannerEstimationStep24()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep24.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;

        (mannerEstimationViewModel.MannerEstimationStep24.ManureType, _) =await FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        mannerEstimationViewModel.MannerEstimationStep24.ApplicationMethodCount = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodCount;
        return mannerEstimationViewModel.MannerEstimationStep24;
    }
    public async Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(MannerEstimationStep24ViewModel mannerEstimationStep24)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep24 = mannerEstimationStep24;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep24();
    }

    public ManureType? GetAndApplyManureType(int manureTypeId, List<ManureType> manureTypeList)
    {
        ManureType? manureType = manureTypeList
            .FirstOrDefault(x => x.Id == manureTypeId);

        return manureType;
    }

    public async Task<MannerEstimationStep25ViewModel> GetMannerEstimationStep25()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep25.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        (List<ManureType> manureTypeList, _) = await FetchManureTypeList(mannerEstimationViewModel.MannerEstimationStep2.CountryID, mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId.Value);
        ManureType? manureType = GetAndApplyManureType(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value, manureTypeList);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep25.MgO = manureType.MgO;
            mannerEstimationViewModel.MannerEstimationStep25.N = manureType.TotalN;
            mannerEstimationViewModel.MannerEstimationStep25.DryMatterPercent = manureType.DryMatter;
            mannerEstimationViewModel.MannerEstimationStep25.P2O5 = manureType.P2O5;
            mannerEstimationViewModel.MannerEstimationStep25.SO3 = manureType.SO3;
            mannerEstimationViewModel.MannerEstimationStep25.K2O = manureType.K2O;
            mannerEstimationViewModel.MannerEstimationStep25.NH4N = manureType.NH4N;
            mannerEstimationViewModel.MannerEstimationStep25.NO3N = manureType.NO3N;
        }
        return mannerEstimationViewModel.MannerEstimationStep25;
    }
    public async Task<MannerEstimationStep25ViewModel> SetMannerEstimationStep25(MannerEstimationStep25ViewModel mannerEstimationStep25)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep25 = mannerEstimationStep25;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep25();
    }
    public async Task<MannerEstimationStep26ViewModel> GetMannerEstimationStep26()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep26.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep26.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        (List<ManureType> manureTypeList, _) = await FetchManureTypeList(mannerEstimationViewModel.MannerEstimationStep2.CountryID, mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId.Value);
        ManureType? manureType = GetAndApplyManureType(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value, manureTypeList);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeLiquid = manureType.IsLiquid;
            mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable = manureType.ApplicationRateArable;
        }
        return mannerEstimationViewModel.MannerEstimationStep26;
    }
    public async Task<MannerEstimationStep26ViewModel> SetMannerEstimationStep26(MannerEstimationStep26ViewModel mannerEstimationStep26)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep26 = mannerEstimationStep26;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep26();
    }
}
