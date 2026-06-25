using Microsoft.AspNetCore.Http;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class MannerEstimationLogic(ILogger<MannerEstimationLogic> logger, IMannerEstimationService mannerEstimationService, IMannerService mannerService, IFieldService fieldService, IFarmService farmService, IOrganicManureLogic organicManureLogic, IHttpContextAccessor httpContextAccessor) : IMannerEstimationLogic
{
    private readonly ILogger<MannerEstimationLogic> _logger = logger;
    private readonly IMannerEstimationService _mannerEstimationService = mannerEstimationService;
    private readonly IMannerService _mannerService = mannerService;
    private readonly IFarmService _farmService = farmService;
    private readonly IFieldService _fieldService = fieldService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
    private const string _mannerEstimationSessionName = "MannerEstimation";
    private const string _dateStringLiteral = "yyyy-MM-dd";

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

    private async Task<int?> FetchFarmRB209CoutryId(int countryId)
    {
        Country? country = await _mannerService.FetchCountryById(countryId);

        if (country == null)
            return null;
        return country.RB209CountryID;
    }
    public async Task<MannerEstimationStep2ViewModel> SetMannerEstimationStep2(MannerEstimationStep2ViewModel mannerEstimationStep2)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2 = mannerEstimationStep2;
        mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = await FetchFarmRB209CoutryId(mannerEstimationViewModel.MannerEstimationStep2.CountryID);
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
        decimal rainfall = await _mannerService.FetchRainfallAverageAsync(firstHalfPostcode);
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

    public async Task<MannerEstimationStep9ViewModel> SetMannerEstimationStep9(MannerEstimationStep9ViewModel mannerEstimationStep9)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep9.CropTypeId != mannerEstimationViewModel.MannerEstimationStep9.CropTypeId)
        {
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = null;
        }
        (CropTypeLinkingResponse cropTypeLinkingResponse, _) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(mannerEstimationStep9.CropTypeId.Value);
        mannerEstimationStep9.MannerCropTypeId = cropTypeLinkingResponse.MannerCropTypeID;
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
        if (mannerEstimationStep13.ApplicationDate != mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate)
        {
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = null;
        }
        mannerEstimationViewModel.MannerEstimationStep13 = mannerEstimationStep13;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep13();
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
        mannerEstimationViewModel.MannerEstimationStep14.IsCopyEstimate = mannerEstimationViewModel.IsCopyEstimate;
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
    public MannerEstimationStep21ViewModel GetMannerEstimationStep21()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep21.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep21;
    }
    public MannerEstimationStep21ViewModel SetMannerEstimationStep21(MannerEstimationStep21ViewModel mannerEstimationStep21)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep21 = mannerEstimationStep21;
        mannerEstimationViewModel.IsCopyEstimate = mannerEstimationStep21.IsCopyEstimate;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep21();
    }
    public MannerEstimationStep22ViewModel GetMannerEstimationStep22()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep22.IsCheckAnswer = mannerEstimationViewModel.IsCheckAnswer;
        return mannerEstimationViewModel.MannerEstimationStep22;
    }
    public MannerEstimationStep22ViewModel SetMannerEstimationStep22(MannerEstimationStep22ViewModel mannerEstimationStep22)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep22 = mannerEstimationStep22;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep22();
    }
    public async Task<(List<MannerEstimation>, Error?)> FetchMannerEstimationsList(Guid orgId)
    {
        _logger.LogTrace("MannerLogic : FetchMannerEstimationsList() by organisation id called");
        return await _mannerEstimationService.FetchMannerEstimationsList(orgId);
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

        (mannerEstimationViewModel.MannerEstimationStep24.ManureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        mannerEstimationViewModel.MannerEstimationStep24.ApplicationMethodCount = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodCount;
        return mannerEstimationViewModel.MannerEstimationStep24;
    }
    public async Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(MannerEstimationStep24ViewModel mannerEstimationStep24)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep24 = mannerEstimationStep24;
        (mannerEstimationViewModel.MannerEstimationStep24.ManureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
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
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
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
            mannerEstimationViewModel.MannerEstimationStep25.UricAcid = manureType.Uric;
            mannerEstimationViewModel.MannerEstimationStep25.IsManureTypeLiquid = manureType.IsLiquid;
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
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
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
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable = manureType.ApplicationRateArable;
        }
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep26();
    }
    public async Task<MannerEstimationStep27ViewModel> GetMannerEstimationStep27()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep27.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep27.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeLiquid = mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeLiquid;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeLiquid = manureType.IsLiquid;
        }
        return mannerEstimationViewModel.MannerEstimationStep27;
    }
    public async Task<MannerEstimationStep27ViewModel> SetMannerEstimationStep27(MannerEstimationStep27ViewModel mannerEstimationStep27)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep27 = mannerEstimationStep27;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep27();
    }

    public async Task<MannerEstimationStep28ViewModel> GetMannerEstimationStep28()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep28.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep28.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep28.IsManureTypeLiquid = manureType.IsLiquid;
        }
        return mannerEstimationViewModel.MannerEstimationStep28;
    }
    public async Task<MannerEstimationStep28ViewModel> SetMannerEstimationStep28(MannerEstimationStep28ViewModel mannerEstimationStep28)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep28 = mannerEstimationStep28;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep28();
    }
    public async Task<Error?> CopiedFarmAndFieldData(int farmId, int fieldId)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        (FarmResponse? farm, Error? error) = await _farmService.FetchFarmByIdAsync(farmId);
        if (farm != null)
        {
            mannerEstimationViewModel.MannerEstimationStep1.FarmName = farm.Name;
            mannerEstimationViewModel.MannerEstimationStep2.CountryID = farm.CountryID.Value;
            mannerEstimationViewModel.MannerEstimationStep3.Postcode = farm.Postcode;
            mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic = farm.RegisteredOrganicProducer;
            mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall = farm.Rainfall.Value;
            Field field = await _fieldService.FetchFieldByFieldIdServiceAsync(fieldId);
            mannerEstimationViewModel.MannerEstimationStep5.FieldName = field.Name;
            mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ = field.IsWithinNVZ;
            (SoilTypeSoilTextureResponse soilTypeSoilTextureResponse, _) = await _organicManureLogic.FetchSoilTypeSoilTextureBySoilTypeIdServiceAsync(field.SoilTypeID.Value);
            mannerEstimationViewModel.MannerEstimationStep18.TopSoilId = soilTypeSoilTextureResponse.TopSoilID;
            mannerEstimationViewModel.MannerEstimationStep19.SubSoilId = soilTypeSoilTextureResponse.SubSoilID;
        }
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return error;
    }
    public MannerEstimationStep29ViewModel GetMannerEstimationStep29()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep29.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep29.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep29.ApplicationMethodId = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId;
        mannerEstimationViewModel.MannerEstimationStep29.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        return mannerEstimationViewModel.MannerEstimationStep29;
    }
    public MannerEstimationStep29ViewModel SetMannerEstimationStep29(MannerEstimationStep29ViewModel mannerEstimationStep29)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep29 = mannerEstimationStep29;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep29();
    }
    public MannerEstimationStep30ViewModel GetMannerEstimationStep30()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep30.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep30.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep30.IncorporationMethodId = mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId;
        return mannerEstimationViewModel.MannerEstimationStep30;
    }
    public MannerEstimationStep30ViewModel SetMannerEstimationStep30(MannerEstimationStep30ViewModel mannerEstimationStep30)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep30 = mannerEstimationStep30;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep30();
    }
    public async Task<bool> FetchIsExistMannerEstimationsByOrgIdAndName(Guid organisationId, string name)
    {
        _logger.LogTrace("ManureLogic : FetchIsExistMannerEstimationsByOrgIdAndName() called");
        return await _mannerEstimationService.FetchIsExistMannerEstimationsByOrgIdAndNameAsyncAPI(organisationId, name);
    }
    public MannerEstimationStep31ViewModel GetMannerEstimationStep31()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep31.IsCopyEstimate = mannerEstimationViewModel.MannerEstimationStep21.IsCopyEstimate;
        return mannerEstimationViewModel.MannerEstimationStep31;
    }
    public MannerEstimationStep31ViewModel SetMannerEstimationStep31(MannerEstimationStep31ViewModel mannerEstimationStep31)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep31 = mannerEstimationStep31;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep31();
    }

    public MannerEstimationStep32ViewModel GetMannerEstimationStep32()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep32.ApplicationMethodId = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId;
        mannerEstimationViewModel.MannerEstimationStep32.IncorporationMethodId = mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId;
        mannerEstimationViewModel.MannerEstimationStep32.ApplicationRateMethod = mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod;
        mannerEstimationViewModel.MannerEstimationStep32.ApplicationDate = mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate;
        mannerEstimationViewModel.MannerEstimationStep32.PostCode = mannerEstimationViewModel.MannerEstimationStep3.Postcode;
        mannerEstimationViewModel.MannerEstimationStep32.CropTypeId = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;
        mannerEstimationViewModel.MannerEstimationStep32.CropTypeName = mannerEstimationViewModel.MannerEstimationStep9.CropTypeName;
        mannerEstimationViewModel.MannerEstimationStep32.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        return mannerEstimationViewModel.MannerEstimationStep32;
    }
    public MannerEstimationStep32ViewModel SetMannerEstimationStep32(MannerEstimationStep32ViewModel mannerEstimationStep32)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep32 = mannerEstimationStep32;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep32();
    }

    public async Task<(bool, Error?)> AddMannerEstimation(Guid organisationId)
    {
        bool success = false; 
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        var mannerEstimate = new MannerEstimation
        {
            Name = mannerEstimationViewModel.MannerEstimationStep31.Name,
            OrganisationID = organisationId,
            FarmName = mannerEstimationViewModel.MannerEstimationStep1.FarmName,
            CountryID = mannerEstimationViewModel.MannerEstimationStep2.CountryID,
            Postcode = mannerEstimationViewModel.MannerEstimationStep3.Postcode,
            AverageAnuualRainfall = mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall,
            FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName,
            IsWithinNVZ = mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ,
            NVZProgrammeID = 2,
            TopSoilID = mannerEstimationViewModel.MannerEstimationStep18.TopSoilId,
            SubSoilID = mannerEstimationViewModel.MannerEstimationStep19.SubSoilId,
            CropTypeID = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId,
            MannerCropTypeID = mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId,
            SowingDate = mannerEstimationViewModel.MannerEstimationStep20.SowingDate
        };

        bool isDefaultnutrient = mannerEstimationViewModel.MannerEstimationStep24.DefaultNutrientValue ?? false;
        decimal? nitrogen = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.TotalN : mannerEstimationViewModel.MannerEstimationStep25.N;
        decimal? p2O5 = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.P2O5 : mannerEstimationViewModel.MannerEstimationStep25.P2O5;
        decimal? k2O = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.K2O : mannerEstimationViewModel.MannerEstimationStep25.K2O;
        decimal? mgO = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.MgO : mannerEstimationViewModel.MannerEstimationStep25.MgO;
        decimal? sO3 = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.SO3 : mannerEstimationViewModel.MannerEstimationStep25.SO3;
        decimal? dryMatter = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.DryMatter : mannerEstimationViewModel.MannerEstimationStep25.DryMatterPercent;
        decimal? uricAcid = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.Uric : mannerEstimationViewModel.MannerEstimationStep25.UricAcid;

        decimal? nH4N = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.NH4N : mannerEstimationViewModel.MannerEstimationStep25.NH4N;
        decimal? nO3N = isDefaultnutrient ? mannerEstimationViewModel.MannerEstimationStep24.ManureType?.NO3N : mannerEstimationViewModel.MannerEstimationStep25.NO3N;

        if (mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
        {
            mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate = mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable;
        }

        if (mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
        {
            mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate = mannerEstimationViewModel.MannerEstimationStep28.ApplicationRate;
        }
        var mannerEstimationApplication = new MannerEstimationApplication
        {
            ManureTypeID = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId,
            ApplicationDate = mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate.Value,

            N = nitrogen,
            P2O5 = p2O5,
            K2O = k2O,
            MgO = mgO,
            SO3 = sO3,

            DryMatterPercent = dryMatter,
            UricAcid = uricAcid ?? 0,

            NH4N = nH4N,
            NO3N = nO3N,

            ApplicationRate = mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate,
            AreaSpread = mannerEstimationViewModel.MannerEstimationStep28.AreaSpread,
            ManureQuantity = mannerEstimationViewModel.MannerEstimationStep28.ManureQuantity,
            ApplicationMethodID = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId,

            IncorporationMethodID = mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId,
            IncorporationDelayID = mannerEstimationViewModel.MannerEstimationStep30.IncorporationDelayId,
            WindspeedID = mannerEstimationViewModel.MannerEstimationStep32.WindspeedId,
            RainfallWithinSixHoursID = mannerEstimationViewModel.MannerEstimationStep32.RainfallWithinSixHoursId,
            MoistureID = mannerEstimationViewModel.MannerEstimationStep32.MoistureTypeId,

            AutumnCropNitrogenUptake = mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake,
            EndOfDrainageDate = mannerEstimationViewModel.MannerEstimationStep32.SoilDrainageEndDate,
            RainfallPostApplication = mannerEstimationViewModel.MannerEstimationStep32.TotalRainfall,

        };

        (string? mannerRequestbody, Error? error) = await BindManureOutput(mannerEstimate, mannerEstimationApplication);
        if (!string.IsNullOrEmpty(error?.Message))
        {
            return (false, error);
        }

        (MannerCalculateNutrientResponse mannerOutput, error) = await _organicManureLogic.FetchMannerCalculateNutrient(mannerRequestbody);
        if (error == null && mannerOutput != null)
        {
            mannerEstimationApplication.TotalN = mannerOutput.TotalN;
            mannerEstimationApplication.CropAvailableNCurrentCrop = mannerOutput.CurrentCropAvailableN;
            mannerEstimationApplication.CropAvailableNitrogenFollowingCropYearTwo = mannerOutput.FollowingCropYear2AvailableN;

            mannerEstimationApplication.TotalP2O5 = mannerOutput.TotalP2O5;
            mannerEstimationApplication.CropAvailableP2O5 = mannerOutput.CropAvailableP2O5;

            mannerEstimationApplication.TotalSO3 = mannerOutput.TotalSO3;
            mannerEstimationApplication.TotalMgO = mannerOutput.TotalMgO;

            mannerEstimationApplication.TotalK2O = mannerOutput.TotalK2O;
            mannerEstimationApplication.CropAvailableK2O = mannerOutput.CropAvailableK2O;

            mannerEstimationApplication.NitrogenUseEfficiency = mannerOutput.NitrogenEfficiencePercentage;

            mannerEstimationApplication.MineralisedNitrogenLosses = mannerOutput.MineralisedN;
            mannerEstimationApplication.LostNitrateLosses = mannerOutput.NitrateNLoss;
            mannerEstimationApplication.LostAmmonia = mannerOutput.AmmoniaNLoss;
            mannerEstimationApplication.LostDenitrified = mannerOutput.DenitrifiedNLoss;
        }

        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerEstimation = mannerEstimate,
            MannerEstimationApplication = mannerEstimationApplication
        });

        (success, error) = await _mannerEstimationService.AddMannerEstimationServiceAsync(jsonData);
        return (success, error);
    }

    private async Task<(string?, Error?)> BindManureOutput(MannerEstimation mannerEstimation, MannerEstimationApplication mannerEstimationApplication)
    {
        Error? error = null;
        bool isMannerScotland = mannerEstimation.CountryID == (int)NMP.Commons.Enums.FarmCountry.Scotland;
        int rb209CountryId = mannerEstimation.CountryID == (int)NMP.Commons.Enums.FarmCountry.England ||
            mannerEstimation.CountryID == (int)NMP.Commons.Enums.FarmCountry.Wales ? (int)NMP.Commons.Enums.RB209Country.England : (int)NMP.Commons.Enums.RB209Country.Scotland;
        string? manureName = string.Empty;
        bool? isLiquid = false;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationApplication.ManureTypeID.Value);
        if (manureType != null)
        {
            manureName = manureType.Name;
            isLiquid = manureType.IsLiquid ?? false;
        }
        var mannerOutput = new
        {
            runType = isMannerScotland ? (int)NMP.Commons.Enums.RunType.MannerScotland : (int)NMP.Commons.Enums.RunType.MannerEngland,
            postcode = mannerEstimation.Postcode.Split(" ")[0],
            countryID = rb209CountryId,
            field = new
            {
                fieldID = 0,
                fieldName = mannerEstimation.FieldName,
                MannerCropTypeID = mannerEstimation.MannerCropTypeID,
                topsoilID = mannerEstimation.TopSoilID,
                subsoilID = mannerEstimation.SubSoilID,
                isInNVZ = mannerEstimation.IsWithinNVZ
            },
            manureApplications = new[]
                                      {
                                                new
                                                {
                                                    manureDetails = new
                                                    {
                                                        manureID = mannerEstimationApplication.ManureTypeID,
                                                        name = manureName,
                                                        isLiquid = isLiquid,
                                                        dryMatter = mannerEstimationApplication.DryMatterPercent,
                                                        totalN = mannerEstimationApplication.N,
                                                        nH4N = mannerEstimationApplication.NH4N,
                                                        uric = mannerEstimationApplication.UricAcid,
                                                        nO3N = mannerEstimationApplication.NO3N,
                                                        p2O5 = mannerEstimationApplication.P2O5,
                                                        k2O = mannerEstimationApplication.K2O,
                                                        sO3 = mannerEstimationApplication.SO3,
                                                        mgO = mannerEstimationApplication.MgO
                                                    },
                                                    applicationDate = mannerEstimationApplication.ApplicationDate.ToString(_dateStringLiteral),
                                                    applicationRate = new
                                                    {
                                                        value = mannerEstimationApplication.ApplicationRate,
                                                        unit = isLiquid.Value ? Resource.lblMeterCubePerHectare : Resource.lblTonnesPerHectare
                                                    },
                                                    applicationMethodID = mannerEstimationApplication.ApplicationMethodID,
                                                    incorporationMethodID = mannerEstimationApplication.IncorporationMethodID,
                                                    incorporationDelayID = mannerEstimationApplication.IncorporationDelayID,
                                                    autumnCropNitrogenUptake = new
                                                    {
                                                        value = mannerEstimationApplication.AutumnCropNitrogenUptake,
                                                        unit = Resource.lblKgPerHectare
                                                    },
                                                    endOfDrainageDate = mannerEstimationApplication.EndOfDrainageDate?.ToString(_dateStringLiteral),
                                                    rainfallPostApplication = mannerEstimationApplication.RainfallPostApplication,
                                                    windspeedID = mannerEstimationApplication.WindspeedID,
                                                    rainTypeID = mannerEstimationApplication.RainfallWithinSixHoursID,
                                                    topsoilMoistureID = mannerEstimationApplication.MoistureID
                                                }
                                            }
        };
        return (JsonConvert.SerializeObject(mannerOutput), error);


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
}

