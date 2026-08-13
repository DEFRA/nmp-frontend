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
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
        mannerEstimationViewModel.MannerEstimationStep1.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep1.IsFarmCopied = mannerEstimationViewModel.MannerEstimationStep15.FarmId != null;
        return mannerEstimationViewModel.MannerEstimationStep1;
    }

    public MannerEstimationStep2ViewModel GetMannerEstimationStep2()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep2.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        if (mannerEstimationViewModel.MannerEstimationStep2.CountryID != mannerEstimationStep2.CountryID)
        {
            mannerEstimationStep2.IsCountryIdChange = true;
        }
        mannerEstimationViewModel.MannerEstimationStep2 = mannerEstimationStep2;
        mannerEstimationViewModel.MannerEstimationStep2.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = await FetchFarmRB209CoutryId(mannerEstimationViewModel.MannerEstimationStep2.CountryID);

        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep2();
    }

    public MannerEstimationStep3ViewModel GetMannerEstimationStep3()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep3.IsCountryIdChange = mannerEstimationViewModel.MannerEstimationStep2.IsCountryIdChange;
        mannerEstimationViewModel.MannerEstimationStep3.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
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
                mannerEstimationStep4ViewModel.IsPostCodeChange = mannerEstimationStep3.IsPostCodeChange;
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

    public MannerEstimationViewModel? GetMannerEstimationFromSession()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var mannerEstimation = session?.GetObjectFromJson<MannerEstimationViewModel>(_mannerEstimationSessionName);

        return mannerEstimation;
    }

    public void SetMannerEstimationToSession(MannerEstimationViewModel mannerEstimationViewModel)
    {
        _httpContextAccessor.HttpContext?.Session.SetObjectAsJson(_mannerEstimationSessionName, mannerEstimationViewModel);
    }



    public async Task<MannerEstimationStep4ViewModel> GetMannerEstimationStep4()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep4.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        mannerEstimationViewModel.MannerEstimationStep5.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep5.MannerFarmId = mannerEstimationViewModel.MannerFarmId;
        return mannerEstimationViewModel.MannerEstimationStep5;
    }
    public MannerEstimationStep5ViewModel SetMannerEstimationStep5(MannerEstimationStep5ViewModel mannerEstimationStep5)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep5 = mannerEstimationStep5;
        mannerEstimationViewModel.MannerEstimationStep5.MannerFarmId = mannerEstimationViewModel.MannerFarmId;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep5();
    }

    public MannerEstimationStep6ViewModel GetMannerEstimationStep6()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep6.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        mannerEstimationViewModel.MannerEstimationStep8.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep8.IsFarmCopied = mannerEstimationViewModel.MannerEstimationStep15.FarmId != null;
        return mannerEstimationViewModel.MannerEstimationStep8;
    }

    public MannerEstimationStep8ViewModel SetMannerEstimationStep8(MannerEstimationStep8ViewModel mannerEstimationStep8)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationStep8.IsFarmCopied = mannerEstimationViewModel.MannerEstimationStep15.FarmId != null;
        if (mannerEstimationViewModel.MannerEstimationStep8.CropGroupId != mannerEstimationStep8.CropGroupId)
        {
            mannerEstimationViewModel.MannerEstimationStep9.CropTypeId = null;
            mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId = null;
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = 0;
            mannerEstimationStep8.IsCropGroupChange = true;
        }
        mannerEstimationViewModel.MannerEstimationStep8 = mannerEstimationStep8;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep8();
    }

    public MannerEstimationStep9ViewModel GetMannerEstimationStep9()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep9.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep9.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep9.IsCropGroupChange = mannerEstimationViewModel.MannerEstimationStep8.IsCropGroupChange;
        mannerEstimationViewModel.MannerEstimationStep9.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        mannerEstimationViewModel.MannerEstimationStep9.CropGroupName = mannerEstimationViewModel.MannerEstimationStep8.CropGroupName;
        return mannerEstimationViewModel.MannerEstimationStep9;
    }

    public async Task<MannerEstimationStep9ViewModel> SetMannerEstimationStep9(MannerEstimationStep9ViewModel mannerEstimationStep9)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep9.CropTypeId != mannerEstimationViewModel.MannerEstimationStep9.CropTypeId)
        {
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = 0;
            mannerEstimationStep9.IsCropTypeChange = true;
        }
        if (mannerEstimationStep9.CropTypeId != null && !Enum.IsDefined(typeof(NMP.Commons.Enums.EarlyOrLateSownCropTypes), mannerEstimationStep9.CropTypeId))
        {
            mannerEstimationViewModel.MannerEstimationStep20.SowingDate = null;
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
        mannerEstimationViewModel.MannerEstimationStep11.EncryptedMannerEstimationId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep11.CropTypeId = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId ?? 0;
        return mannerEstimationViewModel.MannerEstimationStep11;
    }

    public async Task<MannerEstimationStep11ViewModel> SetMannerEstimationStep11(MannerEstimationStep11ViewModel mannerEstimationStep11)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep11.IsComingForAddNewApplication)
        {
            mannerEstimationViewModel.EncryptedMannerEstimationId = mannerEstimationStep11.EncryptedMannerEstimationId;
            mannerEstimationViewModel.IsComingForAddNewApplication = true;
            if (mannerEstimationStep11.IsComingForAddNewApplication)
            {
                mannerEstimationViewModel.CountryId = mannerEstimationStep11.CountryId;
                mannerEstimationViewModel.IsFarmOrganic = mannerEstimationStep11.IsFarmOrganic;
                mannerEstimationViewModel.IsWithinNVZ = mannerEstimationStep11.IsWithinNVZ;
                mannerEstimationViewModel.CropTypeId = mannerEstimationStep11.CropTypeId;
            }

            SetMannerEstimationToSession(mannerEstimationViewModel);
            mannerEstimationViewModel = GetMannerEstimation();
        }

        if (mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId != mannerEstimationStep11.ManureGroupId)
        {
            mannerEstimationStep11.IsManureGroupIdChange = true;
        }
        mannerEstimationViewModel.MannerEstimationStep11.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep11.EncryptedMannerEstimationId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep11 = mannerEstimationStep11;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep11();
    }

    public MannerEstimationStep12ViewModel GetMannerEstimationStep12()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep12.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep12.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep12.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep12.ManureGroupName = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupName;
        mannerEstimationViewModel.MannerEstimationStep12.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep12.IsManureGroupIdChange = mannerEstimationViewModel.MannerEstimationStep11.IsManureGroupIdChange;
        return mannerEstimationViewModel.MannerEstimationStep12;
    }

    public MannerEstimationStep12ViewModel SetMannerEstimationStep12(MannerEstimationStep12ViewModel mannerEstimationStep12)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationStep12.IsManureGroupIdChange = mannerEstimationViewModel.MannerEstimationStep11.IsManureGroupIdChange;
        if (mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId != mannerEstimationStep12.ManureTypeId)
        {
            mannerEstimationStep12.IsManureTypeChange = true;
        }
        mannerEstimationStep12.IsManureGroupIdChange = mannerEstimationViewModel.MannerEstimationStep11.IsManureGroupIdChange;
        mannerEstimationViewModel.MannerEstimationStep12 = mannerEstimationStep12;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep12();
    }

    public MannerEstimationStep13ViewModel GetMannerEstimationStep13()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();

        mannerEstimationViewModel.MannerEstimationStep13.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep13.EncryptedMannerApplicationId = mannerEstimationViewModel.EncryptedMannerEstimationApplicationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep13.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep13.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep13.CountryId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CountryId ?? 0 : mannerEstimationViewModel.MannerEstimationStep2.CountryID;
        mannerEstimationViewModel.MannerEstimationStep13.FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName;
        mannerEstimationViewModel.MannerEstimationStep13.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;

        mannerEstimationViewModel.MannerEstimationStep13.CropTypeId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CropTypeId : mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;
        mannerEstimationViewModel.MannerEstimationStep13.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        mannerEstimationViewModel.MannerEstimationStep13.TopSoilId = mannerEstimationViewModel.MannerEstimationStep18.TopSoilId;
        mannerEstimationViewModel.MannerEstimationStep13.SubSoilId = mannerEstimationViewModel.MannerEstimationStep19.SubSoilId;
        mannerEstimationViewModel.MannerEstimationStep13.SowingDate = mannerEstimationViewModel.MannerEstimationStep20.SowingDate;
        mannerEstimationViewModel.MannerEstimationStep13.IsFarmOrganic = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsFarmOrganic : mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic;
        mannerEstimationViewModel.MannerEstimationStep13.IsWithinNVZ = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsWithinNVZ : mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ;
        mannerEstimationViewModel.MannerEstimationStep13.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep13.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId;

        mannerEstimationViewModel.MannerEstimationStep13.MannerEstimationId = mannerEstimationViewModel.MannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep13.MannerEstimationApplicationsId = mannerEstimationViewModel.MannerEstimationApplicationId;
        mannerEstimationViewModel.MannerEstimationStep13.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        return mannerEstimationViewModel.MannerEstimationStep13;
    }

    public MannerEstimationStep13ViewModel SetMannerEstimationStep13(MannerEstimationStep13ViewModel mannerEstimationStep13)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep13.ApplicationDate != mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate)
        {
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = 0;
            mannerEstimationStep13.IsApplicationDateChange = true;
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
        mannerEstimationViewModel.MannerEstimationStep17.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        mannerEstimationViewModel.MannerEstimationStep18.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        mannerEstimationViewModel.MannerEstimationStep19.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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
        mannerEstimationViewModel.MannerEstimationStep20.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        mannerEstimationViewModel.MannerEstimationStep20.IsCropTypeChange = mannerEstimationViewModel.MannerEstimationStep9.IsCropTypeChange;
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
    public async Task<(List<MannerEstimationDetailsViewModel>, Error?)> FetchMannerEstimationsList(Guid orgId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerEstimationsList() by organisation id called");
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

        mannerEstimationViewModel.MannerEstimationStep23.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep23.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep23.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        return mannerEstimationViewModel.MannerEstimationStep23;
    }
    public async Task<MannerEstimationStep23ViewModel> SetMannerEstimationStep23(MannerEstimationStep23ViewModel mannerEstimationStep23)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep23.ApplicationMethodId != mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId)
        {
            mannerEstimationStep23.IsApplicationMethodChange = true;
            mannerEstimationViewModel.MannerEstimationStep29.IsApplicationMethodChange = true;
        }

        mannerEstimationStep23.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
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

        mannerEstimationViewModel.MannerEstimationStep24.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep24.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep24.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        return mannerEstimationViewModel.MannerEstimationStep24;
    }
    public async Task<MannerEstimationStep24ViewModel> SetMannerEstimationStep24(MannerEstimationStep24ViewModel mannerEstimationStep24)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep24 = mannerEstimationStep24;
        (mannerEstimationViewModel.MannerEstimationStep24.ManureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        mannerEstimationViewModel.MannerEstimationStep24.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
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

        mannerEstimationViewModel.MannerEstimationStep25.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep25.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep25.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
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
        mannerEstimationViewModel.MannerEstimationStep25.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep25();
    }
    public async Task<MannerEstimationStep26ViewModel> GetMannerEstimationStep26()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep26.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep26.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;

        mannerEstimationViewModel.MannerEstimationStep26.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep26.EncryptedMannerApplicationsId = mannerEstimationViewModel.EncryptedMannerEstimationApplicationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep26.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeLiquid = manureType.IsLiquid;
            mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable = manureType.ApplicationRateArable;
        }
        mannerEstimationViewModel.MannerEstimationStep26.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep26.CountryId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CountryId ?? 0 : mannerEstimationViewModel.MannerEstimationStep2.CountryID;
        mannerEstimationViewModel.MannerEstimationStep26.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        mannerEstimationViewModel.MannerEstimationStep26.CropTypeId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CropTypeId : mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;

        mannerEstimationViewModel.MannerEstimationStep26.IsFarmOrganic = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsFarmOrganic : mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic;
        mannerEstimationViewModel.MannerEstimationStep26.IsWithinNVZ = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsWithinNVZ : mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ;

        mannerEstimationViewModel.MannerEstimationStep26.ApplicationDate = mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate;
        mannerEstimationViewModel.MannerEstimationStep26.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId;
        mannerEstimationViewModel.MannerEstimationStep26.DefaultNutrientValue = mannerEstimationViewModel.MannerEstimationStep24.DefaultNutrientValue;
        return mannerEstimationViewModel.MannerEstimationStep26;
    }
    public async Task<MannerEstimationStep26ViewModel> SetMannerEstimationStep26(MannerEstimationStep26ViewModel mannerEstimationStep26)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep26 = mannerEstimationStep26;
        mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
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

        mannerEstimationViewModel.MannerEstimationStep27.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep27.EncryptedMannerApplicationsId = mannerEstimationViewModel.EncryptedMannerEstimationApplicationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep27.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep27.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep27.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeLiquid = mannerEstimationViewModel.MannerEstimationStep26.IsManureTypeLiquid;

        mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeLiquid = manureType.IsLiquid;
        }

        mannerEstimationViewModel.MannerEstimationStep27.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep27.CountryId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CountryId ?? 0 : mannerEstimationViewModel.MannerEstimationStep2.CountryID;
        mannerEstimationViewModel.MannerEstimationStep27.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;

        mannerEstimationViewModel.MannerEstimationStep27.CropTypeId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CropTypeId : mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;
        mannerEstimationViewModel.MannerEstimationStep27.IsFarmOrganic = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsFarmOrganic : mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic;
        mannerEstimationViewModel.MannerEstimationStep27.IsWithinNVZ = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsWithinNVZ : mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ;

        mannerEstimationViewModel.MannerEstimationStep27.ApplicationDate = mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate;
        mannerEstimationViewModel.MannerEstimationStep27.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId;
        return mannerEstimationViewModel.MannerEstimationStep27;
    }
    public async Task<MannerEstimationStep27ViewModel> SetMannerEstimationStep27(MannerEstimationStep27ViewModel mannerEstimationStep27)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep27 = mannerEstimationStep27;
        mannerEstimationViewModel.MannerEstimationStep27.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep27();
    }

    public async Task<MannerEstimationStep28ViewModel> GetMannerEstimationStep28()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();

        mannerEstimationViewModel.MannerEstimationStep28.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep28.EncryptedMannerApplicationsId = mannerEstimationViewModel.EncryptedMannerEstimationApplicationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep28.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep28.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        mannerEstimationViewModel.MannerEstimationStep28.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep28.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId.Value);
        if (manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep28.IsManureTypeLiquid = manureType.IsLiquid;
        }
        mannerEstimationViewModel.MannerEstimationStep28.FarmRB209CountryId = mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId ?? 0;
        mannerEstimationViewModel.MannerEstimationStep28.CountryId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CountryId ?? 0 : mannerEstimationViewModel.MannerEstimationStep2.CountryID;
        mannerEstimationViewModel.MannerEstimationStep28.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;

        mannerEstimationViewModel.MannerEstimationStep28.CropTypeId = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.CropTypeId : mannerEstimationViewModel.MannerEstimationStep9.CropTypeId;
        mannerEstimationViewModel.MannerEstimationStep28.IsFarmOrganic = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsFarmOrganic : mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic;
        mannerEstimationViewModel.MannerEstimationStep28.IsWithinNVZ = mannerEstimationViewModel.IsComingForAddNewApplication ? mannerEstimationViewModel.IsWithinNVZ : mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ;

        mannerEstimationViewModel.MannerEstimationStep28.ApplicationDate = mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate;
        mannerEstimationViewModel.MannerEstimationStep28.ManureGroupId = mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId;
        return mannerEstimationViewModel.MannerEstimationStep28;
    }
    public async Task<MannerEstimationStep28ViewModel> SetMannerEstimationStep28(MannerEstimationStep28ViewModel mannerEstimationStep28)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep28 = mannerEstimationStep28;
        mannerEstimationViewModel.MannerEstimationStep28.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
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
            mannerEstimationViewModel.MannerEstimationStep2.CountryID = farm.CountryID ?? 0;
            mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = farm.RB209CountryID ?? 0;
        }
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return error;
    }
    public MannerEstimationStep29ViewModel GetMannerEstimationStep29()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep29.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        mannerEstimationViewModel.MannerEstimationStep29.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep29.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep29.ApplicationMethodId = mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId;
        mannerEstimationViewModel.MannerEstimationStep29.CropGroupId = mannerEstimationViewModel.MannerEstimationStep8.CropGroupId;
        mannerEstimationViewModel.MannerEstimationStep29.ApplicationRateMethod = mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod;

        mannerEstimationViewModel.MannerEstimationStep29.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep29.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        return mannerEstimationViewModel.MannerEstimationStep29;
    }
    public MannerEstimationStep29ViewModel SetMannerEstimationStep29(MannerEstimationStep29ViewModel mannerEstimationStep29)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId != mannerEstimationStep29.IncorporationMethodId)
        {
            mannerEstimationStep29.IsIncorporationMethodChange = true;
        }
        mannerEstimationViewModel.MannerEstimationStep29 = mannerEstimationStep29;
        mannerEstimationViewModel.MannerEstimationStep29.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep29();
    }
    public MannerEstimationStep30ViewModel GetMannerEstimationStep30()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep30.ManureTypeName = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName;
        mannerEstimationViewModel.MannerEstimationStep30.ManureTypeId = mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId;
        mannerEstimationViewModel.MannerEstimationStep30.IncorporationMethodId = mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId;

        mannerEstimationViewModel.MannerEstimationStep30.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep30.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep30.IsIncorporationMethodChange = mannerEstimationViewModel.MannerEstimationStep29.IsIncorporationMethodChange;

        mannerEstimationViewModel.MannerEstimationStep30.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        return mannerEstimationViewModel.MannerEstimationStep30;
    }
    public MannerEstimationStep30ViewModel SetMannerEstimationStep30(MannerEstimationStep30ViewModel mannerEstimationStep30)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep30 = mannerEstimationStep30;
        mannerEstimationViewModel.MannerEstimationStep30.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep30();
    }
    public async Task<bool> FetchIsExistMannerEstimationsByMannerFarmIdAndName(int mannerFarmId, string name)
    {
        _logger.LogTrace("ManureLogic : FetchIsExistMannerEstimationsByMannerFarmIdAndName() called");
        return await _mannerEstimationService.FetchIsExistMannerEstimationsByMannerFarmIdAndNameAsyncAPI(mannerFarmId, name);
    }
    public MannerEstimationStep31ViewModel GetMannerEstimationStep31()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep31.EncryptedMannerFarmId = mannerEstimationViewModel.EncryptedMannerFarmId;
        if (mannerEstimationViewModel.IsCopyEstimate == null)
        {
            mannerEstimationViewModel.MannerEstimationStep31.IsCopyEstimate = mannerEstimationViewModel.MannerEstimationStep21.IsCopyEstimate;

        }
        else
        {
            mannerEstimationViewModel.MannerEstimationStep31.IsCopyEstimate = mannerEstimationViewModel.IsCopyEstimate;
        }
        mannerEstimationViewModel.MannerEstimationStep31.EncryptedMannerEstimationId = mannerEstimationViewModel.EncryptedMannerEstimationId;
        if (mannerEstimationViewModel.MannerEstimationStep31.MannerEstimationId == null)
        {
            mannerEstimationViewModel.MannerEstimationStep31.MannerEstimationId = mannerEstimationViewModel.MannerEstimationStep22.MannerEstimationId;
        }

        return mannerEstimationViewModel.MannerEstimationStep31;
    }
    public MannerEstimationStep31ViewModel SetMannerEstimationStep31(MannerEstimationStep31ViewModel mannerEstimationStep31)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (mannerEstimationStep31.IsCopyEstimate == true)
        {
            mannerEstimationViewModel.EncryptedMannerEstimationId = mannerEstimationStep31.EncryptedMannerEstimationId;
            mannerEstimationViewModel.IsCopyEstimate = mannerEstimationStep31.IsCopyEstimate;
        }
        mannerEstimationViewModel.MannerEstimationStep31 = mannerEstimationStep31;
        mannerEstimationViewModel.MannerEstimationStep31.EncryptedMannerEstimationId = mannerEstimationViewModel.EncryptedMannerEstimationId;
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

        mannerEstimationViewModel.MannerEstimationStep32.EncryptedMannerEstimateId = mannerEstimationViewModel.EncryptedMannerEstimationId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep32.IsComingForAddNewApplication = mannerEstimationViewModel.IsComingForAddNewApplication;
        mannerEstimationViewModel.MannerEstimationStep32.IsApplicationDateChange = mannerEstimationViewModel.MannerEstimationStep13.IsApplicationDateChange;

        mannerEstimationViewModel.MannerEstimationStep32.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        return mannerEstimationViewModel.MannerEstimationStep32;
    }
    public MannerEstimationStep32ViewModel SetMannerEstimationStep32(MannerEstimationStep32ViewModel mannerEstimationStep32)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep32 = mannerEstimationStep32;
        mannerEstimationViewModel.MannerEstimationStep32.IsManureTypeChange = mannerEstimationViewModel.MannerEstimationStep12.IsManureTypeChange;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep32();
    }

    public async Task<(MannerEstimationApplication?, Error?)> AddMannerEstimation(Guid organisationId)
    {
        (MannerEstimationViewModel mannerEstimationViewModel, MannerEstimation mannerEstimate, MannerFarm mannerFarm) = await BindMannerEstimationDataForAdd(organisationId, null);

        MannerEstimationApplication? mannerEstimationApplication = await BindMannerEstinationApplicationData(mannerEstimationViewModel, false);

        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerFarm = mannerFarm,
            MannerEstimation = mannerEstimate,
            MannerEstimationApplication = mannerEstimationApplication
        });

        (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error) = await _mannerEstimationService.AddMannerEstimationServiceAsync(jsonData);
        return (mannerEstimationApplicationResult, error);
    }
    public async Task<(MannerFarmEstimationApplicationResponse?, Error?)> AddMannerFarmEstimation(Guid organisationId)
    {
        (MannerEstimationViewModel mannerEstimationViewModel, MannerEstimation mannerEstimate, MannerFarm mannerFarm) = await BindMannerEstimationDataForAdd(organisationId, null);

        MannerEstimationApplication? mannerEstimationApplication = await BindMannerEstinationApplicationData(mannerEstimationViewModel, false);

        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerFarm = mannerFarm,
            MannerEstimation = mannerEstimate,
            MannerEstimationApplication = mannerEstimationApplication
        });

        (MannerFarmEstimationApplicationResponse? mannerFarmEstimationApplicationResult, Error? error) = await _mannerEstimationService.AddMannerFarmEstimationServiceAsync(jsonData);
        return (mannerFarmEstimationApplicationResult, error);
    }

    private async Task<MannerEstimationApplication?> BindMannerEstinationApplicationData(MannerEstimationViewModel mannerEstimationViewModel, bool isUpdate)
    {
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

        BindApplicationRateForUpdate(mannerEstimationViewModel);
        MannerEstimationApplication mannerEstimationApplication = new MannerEstimationApplication
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


        if (isUpdate)
        {
            (MannerEstimationApplication? mannerEstimateApplicationData, _) = await FetchMannerEstimateApplicationById(mannerEstimationViewModel.MannerEstimationApplicationId.Value);
            if (mannerEstimateApplicationData != null)
            {
                mannerEstimationApplication.ID = mannerEstimateApplicationData.ID;
                mannerEstimationApplication.MannerEstimationID = mannerEstimateApplicationData.MannerEstimationID;
                mannerEstimationApplication.NitrogenValue = mannerEstimateApplicationData.NitrogenValue;
                mannerEstimationApplication.PhosphateValue = mannerEstimateApplicationData.PhosphateValue;
                mannerEstimationApplication.PotashValue = mannerEstimateApplicationData.PotashValue;
            }
        }

        if (mannerEstimationViewModel.IsComingForAddNewApplication)
        {
            mannerEstimationApplication.MannerEstimationID = mannerEstimationViewModel.MannerEstimationId;
        }

        return mannerEstimationApplication;
    }


    private static void BindApplicationRateForUpdate(MannerEstimationViewModel mannerEstimationViewModel)
    {
        if (mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate)
        {
            mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate = mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable;
        }

        if (mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod == (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity)
        {
            mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate = mannerEstimationViewModel.MannerEstimationStep28.ApplicationRate;
        }
    }

    private async Task<(MannerEstimationViewModel, MannerEstimation, MannerFarm)> BindMannerEstimationDataForAdd(Guid? organisationId, int? mannerEstimationId)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        MannerFarm mannerFarm = new MannerFarm();
        MannerEstimation mannerEstimate = new MannerEstimation
        {
            MannerFarmID = mannerEstimationViewModel.MannerFarmId,
            Name = mannerEstimationViewModel.MannerEstimationStep31.Name,
            FieldName = mannerEstimationViewModel.MannerEstimationStep5.FieldName,
            IsWithinNVZ = mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ,
            TopSoilID = mannerEstimationViewModel.MannerEstimationStep18.TopSoilId,
            SubSoilID = mannerEstimationViewModel.MannerEstimationStep19.SubSoilId,
            CropTypeID = mannerEstimationViewModel.MannerEstimationStep9.CropTypeId,
            MannerCropTypeID = mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId,
            SowingDate = Enum.IsDefined(
                typeof(NMP.Commons.Enums.EarlyOrLateSownCropTypes),
                mannerEstimationViewModel.MannerEstimationStep9.CropTypeId)
                    ? mannerEstimationViewModel.MannerEstimationStep20.SowingDate
                    : null,
        };

        if (mannerEstimationId != null)
        {
            (MannerEstimation? mannerEstimateData, _) = await FetchMannerEstimateById(mannerEstimationId.Value);
            if (mannerEstimateData != null)
            {
                (MannerFarmViewModel? mannerFarmData, _) = await FetchMannerFarmById(mannerEstimate.MannerFarmID.Value);
                mannerEstimate.ID = mannerEstimationId;
                mannerFarm.OrganisationID = mannerFarmData?.OrganisationID;

                mannerEstimate.NitrogenPrice = mannerEstimateData.NitrogenPrice;
                mannerEstimate.NitrogenProductId = mannerEstimateData.NitrogenProductId;
                mannerEstimate.NitrogenProductName = mannerEstimateData.NitrogenProductName;
                mannerEstimate.NitrogenProductPrice = mannerEstimateData.NitrogenProductPrice;

                mannerEstimate.PotashProductId = mannerEstimateData.PotashProductId;
                mannerEstimate.PotashProductPrice = mannerEstimateData.PotashProductPrice;
                mannerEstimate.PotashProductName = mannerEstimateData.PotashProductName;
                mannerEstimate.PotashPrice = mannerEstimateData.PotashPrice;

                mannerEstimate.PhosphateProductPrice = mannerEstimateData.PhosphateProductPrice;
                mannerEstimate.PhosphateProductId = mannerEstimateData.PhosphateProductId;
                mannerEstimate.PhosphatePrice = mannerEstimateData.PhosphatePrice;
                mannerEstimate.PhosphateProductName = mannerEstimateData.PhosphateProductName;
            }
        }
        else if (organisationId != null)
        {
            mannerFarm.Name = mannerEstimationViewModel.MannerEstimationStep1.FarmName;
            mannerFarm.CountryID = mannerEstimationViewModel.MannerEstimationStep2.CountryID;
            mannerFarm.Postcode = mannerEstimationViewModel.MannerEstimationStep3.Postcode;
            mannerFarm.AverageAnuualRainfall = mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall;
            mannerFarm.RegisteredOrganicProducer = mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic;
            mannerFarm.OrganisationID = organisationId.Value;
        }

        return (mannerEstimationViewModel, mannerEstimate, mannerFarm);
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

    public async Task<(int?, Error?)> FetchSoilTypeSoilTextureByTopSoilSubSoilId(int topSoilId, int subSoilId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchSoilTypeSoilTextureByTopSoilSubSoilId() called");
        return await _mannerEstimationService.FetchSoilTypeSoilTextureByTopSoilSubSoilId(topSoilId, subSoilId);
    }
    public async Task<(List<MannerEstimationApplication>, Error?)> FetchMannerApplicationsByMannerEstimationId(int mannerEstimationId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerApplicationsByMannerEstimationId() called");
        return await _mannerEstimationService.FetchMannerApplicationsByMannerEstimationId(mannerEstimationId);
    }
    public async Task<(MannerEstimationApplication, Error?)> FetchMannerApplicationById(int mannerApplicationId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerApplicationById() called");
        return await _mannerEstimationService.FetchMannerApplicationById(mannerApplicationId);
    }
    public async Task<(MannerEstimationResultResponse?, Error?)> FetchMannerApplicationResultById(int mannerEstimationId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerApplicationResultById() called");
        return await _mannerEstimationService.FetchMannerApplicationResultById(mannerEstimationId);
    }

    public async Task<(int, Error?)> CopyMannerEstimation(int id, string estimationName)
    {
        _logger.LogTrace("MannerEstimationLogic : CopyMannerEstimation() called");
        return await _mannerEstimationService.CopyMannerEstimation(id, estimationName);
    }
    public async Task<bool> FetchDefaultNutrientValue(
    int manureTypeId,
    MannerEstimationApplication mannerEstimationApplication)
    {
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(manureTypeId);

        if (manureType == null)
            return false;

        return
            mannerEstimationApplication.DryMatterPercent == manureType.DryMatter &&
            mannerEstimationApplication.N == manureType.TotalN &&
            mannerEstimationApplication.NH4N == manureType.NH4N &&
            mannerEstimationApplication.UricAcid == manureType.Uric &&
            mannerEstimationApplication.NO3N == manureType.NO3N &&
            mannerEstimationApplication.P2O5 == manureType.P2O5 &&
            mannerEstimationApplication.K2O == manureType.K2O &&
            mannerEstimationApplication.SO3 == manureType.SO3 &&
            mannerEstimationApplication.MgO == manureType.MgO;
    }
    public async Task<(bool, int)> FetchApplicationRateOptionValue(
    int manureTypeId,
    MannerEstimationApplication mannerEstimationApplication, MannerEstimation mannerEstimation)
    {
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(manureTypeId);

        if (manureType == null)
            return (false, 0);

        int? defaultRate = mannerEstimation.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass ? manureType.ApplicationRateGrass : manureType.ApplicationRateArable;

        return
            (mannerEstimationApplication.ApplicationRate == defaultRate, defaultRate ?? 0);
    }
    public async Task<bool> FetchIsManureLiquid(int manureTypeId)
    {
        (ManureType? manureType, _) = await _mannerService.FetchManureTypeByManureTypeId(manureTypeId);

        if (manureType == null)
            return false;

        return manureType.IsLiquid ?? false;
    }

    public MannerEstimationStep33ViewModel GetMannerEstimationStep33()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep33.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;

        return mannerEstimationViewModel.MannerEstimationStep33;
    }
    public MannerEstimationStep33ViewModel SetMannerEstimationStep33(MannerEstimationStep33ViewModel mannerEstimationStep33)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep33 = mannerEstimationStep33;
        mannerEstimationViewModel.MannerEstimationStep33.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep34.MannerEstimateId = mannerEstimationStep33.MannerEstimateId ?? 00;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep33();
    }
    public async Task<MannerEstimationStep34ViewModel> GetMannerEstimationStep34()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep34.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep34.UpdateNitrogenPriceQuestion = mannerEstimationViewModel.MannerEstimationStep33.UpdateNitrogenPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep34.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep34.NutrientProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Nitrogen, mannerEstimationViewModel.MannerEstimationStep34.NutrientProductId ?? 0);
        if (!mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime)
        {
            mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime = true;
            await BindNutrientPrice(mannerEstimationViewModel, (int)NMP.Commons.Enums.MannerNutrients.Nitrogen);
            SetMannerEstimationToSession(mannerEstimationViewModel);
        }
        return mannerEstimationViewModel.MannerEstimationStep34;
    }
    private async Task BindNutrientPrice(MannerEstimationViewModel mannerEstimationViewModel, int nutrientId)
    {
        (MannerEstimation? mannerEstimation, _) = await FetchMannerEstimateById(mannerEstimationViewModel.MannerEstimationStep35.MannerEstimateId.Value);
        if (mannerEstimation != null)
        {
            if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Nitrogen)
            {
                mannerEstimationViewModel.MannerEstimationStep34.NitrogenPrice = mannerEstimation.NitrogenPrice;
                mannerEstimationViewModel.MannerEstimationStep34.NitrogenProductPrice = mannerEstimation.NitrogenProductPrice;
            }
            else if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Phosphorus)
            {
                mannerEstimationViewModel.MannerEstimationStep37.PhosphorusPrice = mannerEstimation.PhosphatePrice;
                mannerEstimationViewModel.MannerEstimationStep37.PhosphorusProductPrice = mannerEstimation.PhosphateProductPrice;
            }
            else if (nutrientId == (int)NMP.Commons.Enums.MannerNutrients.Potassium)
            {
                mannerEstimationViewModel.MannerEstimationStep39.PotashPrice = mannerEstimation.PotashPrice;
                mannerEstimationViewModel.MannerEstimationStep39.PotashProductPrice = mannerEstimation.PotashProductPrice;
            }

        }
    }
    public async Task<MannerEstimationStep34ViewModel> SetMannerEstimationStep34(MannerEstimationStep34ViewModel mannerEstimationStep34)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep34.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep34.UpdateNitrogenPriceQuestion = mannerEstimationStep34.UpdateNitrogenPriceQuestion ?? mannerEstimationViewModel.MannerEstimationStep33.UpdateNitrogenPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep34.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep34.MannerEstimateId = mannerEstimationStep34.MannerEstimateId;
        mannerEstimationStep34.IsComingFirstTime = mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime;
        if (!mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime)
        {
            mannerEstimationStep34.IsComingFirstTime = true;
        }
        decimal nutrientPercentage = await FetchNutrientPrecentage((int)NMP.Commons.Enums.MannerNutrients.Nitrogen, mannerEstimationViewModel.MannerEstimationStep34.NutrientProductId.Value);
        if (mannerEstimationViewModel.MannerEstimationStep34.UpdateNitrogenPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice)
        {
            decimal cal1 = nutrientPercentage / 100m;
            decimal cal2 = cal1 * 1000m;
            mannerEstimationStep34.NitrogenProductPrice =
            (int)Math.Round(cal2 * (mannerEstimationStep34.NitrogenPrice ?? 0));
        }
        else
        {
            decimal cal1 = (nutrientPercentage / 100) * 1000;
            decimal cal2 = (mannerEstimationStep34.NitrogenProductPrice ?? 0 / cal1);
            decimal cal3 = (cal2 / cal1) * 100;
            mannerEstimationStep34.NitrogenPrice = Math.Round(cal3 / 100, 2);
        }

        mannerEstimationViewModel.MannerEstimationStep34 = mannerEstimationStep34;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep34();
    }
    private async Task<decimal> FetchNutrientPrecentage(int nutrientId, int UpdateNitrogenPriceQuestionId)
    {
        decimal percentage = 0.0m;
        (List<NutrientProductResponse> nutrientProducts, _) = await _mannerEstimationService.FetchNutrientProductByNutrientId(nutrientId);
        if (nutrientProducts.Count > 0)
        {
            percentage = nutrientProducts
   .FirstOrDefault(x => x.id == UpdateNitrogenPriceQuestionId)
   ?.nutrientPercentage ?? 0m;
        }
        return percentage;
    }

    private async Task<string> FetchNutrientProductName(int nutrientId, int nutrientProductId)
    {
        string productName = string.Empty;
        (List<NutrientProductResponse> nutrientProducts, _) = await _mannerEstimationService.FetchNutrientProductByNutrientId(nutrientId);
        if (nutrientProducts.Count > 0)
        {
            productName = nutrientProducts
   .FirstOrDefault(x => x.id == nutrientProductId)
   ?.name ?? string.Empty;
        }
        return productName;
    }
    public MannerEstimationStep35ViewModel GetMannerEstimationStep35()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return mannerEstimationViewModel.MannerEstimationStep35;
    }
    public MannerEstimationStep35ViewModel SetMannerEstimationStep35(MannerEstimationStep35ViewModel mannerEstimationStep35)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        if (!string.IsNullOrWhiteSpace(mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId))
        {
            mannerEstimationStep35.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId;
        }
        if (mannerEstimationViewModel.MannerEstimationStep35.NutrientId != null)
        {
            mannerEstimationStep35.NutrientId = mannerEstimationViewModel.MannerEstimationStep35.NutrientId;
        }
        mannerEstimationViewModel.MannerEstimationStep35 = mannerEstimationStep35;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep35();
    }

    public async Task<(List<NutrientProductResponse>, Error?)> FetchNutrientProductByNutrientId(int nurteintId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchNutrientProductByNutrientId() called");
        return await _mannerEstimationService.FetchNutrientProductByNutrientId(nurteintId);
    }
    public async Task<(MannerEstimation?, Error?)> FetchMannerEstimateById(int mannerEstimateId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerEstimateById() called");
        return await _mannerEstimationService.FetchMannerEstimateById(mannerEstimateId);
    }
    public async Task<(MannerEstimation?, Error?)> UpdateMannerEstimation(int MannerEstimationId)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        (MannerEstimation? mannerEstimation, Error? error) = await FetchMannerEstimateById(MannerEstimationId);
        if (!string.IsNullOrWhiteSpace(error?.Message) || mannerEstimation == null)
        {
            return (null, error);
        }
        if (mannerEstimationViewModel.MannerEstimationStep35.NutrientId == ((int)NMP.Commons.Enums.MannerNutrients.Nitrogen))
        {
            mannerEstimation.NitrogenProductPrice = mannerEstimationViewModel.MannerEstimationStep34.NitrogenProductPrice ?? 0;
            mannerEstimation.NitrogenPrice = mannerEstimationViewModel.MannerEstimationStep34.NitrogenPrice ?? 0;
            mannerEstimation.NitrogenProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId ?? 0;
            mannerEstimation.NitrogenProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Nitrogen, mannerEstimation.NitrogenProductId);
            mannerEstimation.IsNitrogenPriceBasedOnNutrientPrice = mannerEstimationViewModel.MannerEstimationStep33.UpdateNitrogenPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
        }
        if (mannerEstimationViewModel.MannerEstimationStep35.NutrientId == ((int)NMP.Commons.Enums.MannerNutrients.Phosphorus))
        {
            mannerEstimation.PhosphateProductPrice = mannerEstimationViewModel.MannerEstimationStep37.PhosphorusProductPrice ?? 0;
            mannerEstimation.PhosphatePrice = mannerEstimationViewModel.MannerEstimationStep37.PhosphorusPrice ?? 0;
            mannerEstimation.PhosphateProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId ?? 0;
            mannerEstimation.PhosphateProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Phosphorus, mannerEstimation.PhosphateProductId);
            mannerEstimation.IsPhosphatePriceBasedOnNutrientPrice = mannerEstimationViewModel.MannerEstimationStep36.UpdatePhosphorusPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
        }
        if (mannerEstimationViewModel.MannerEstimationStep35.NutrientId == ((int)NMP.Commons.Enums.MannerNutrients.Potassium))
        {
            mannerEstimation.PotashProductPrice = mannerEstimationViewModel.MannerEstimationStep39.PotashProductPrice ?? 0;
            mannerEstimation.PotashPrice = mannerEstimationViewModel.MannerEstimationStep39.PotashPrice ?? 0;
            mannerEstimation.PotashProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId ?? 0;
            mannerEstimation.PotashProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Potassium, mannerEstimation.PotashProductId);
            mannerEstimation.IsPotashPriceBasedOnNutrientPrice = mannerEstimationViewModel.MannerEstimationStep38.UpdatePotashPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice;
        }
        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerEstimation = mannerEstimation
        });

        (MannerEstimation? mannerEstimationResult, error) = await _mannerEstimationService.UpdateMannerEstimationServiceAsync(jsonData);
        return (mannerEstimationResult, error);
    }
    public MannerEstimationStep36ViewModel GetMannerEstimationStep36()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep36.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        return mannerEstimationViewModel.MannerEstimationStep36;
    }
    public MannerEstimationStep36ViewModel SetMannerEstimationStep36(MannerEstimationStep36ViewModel mannerEstimationStep36)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep36 = mannerEstimationStep36;
        mannerEstimationViewModel.MannerEstimationStep36.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep37.MannerEstimateId = mannerEstimationStep36.MannerEstimateId ?? 00;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep36();
    }
    public async Task<MannerEstimationStep37ViewModel> GetMannerEstimationStep37()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep37.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep37.UpdatePhosphorusPriceQuestion = mannerEstimationViewModel.MannerEstimationStep36.UpdatePhosphorusPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep37.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep37.NutrientProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Phosphorus, mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId ?? 0);
        if (!mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime)
        {
            mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime = true;
            await BindNutrientPrice(mannerEstimationViewModel, (int)NMP.Commons.Enums.MannerNutrients.Phosphorus);
            SetMannerEstimationToSession(mannerEstimationViewModel);
        }
        return mannerEstimationViewModel.MannerEstimationStep37;
    }
    public async Task<MannerEstimationStep37ViewModel> SetMannerEstimationStep37(MannerEstimationStep37ViewModel mannerEstimationStep37)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep37.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep37.UpdatePhosphorusPriceQuestion = mannerEstimationStep37.UpdatePhosphorusPriceQuestion ?? mannerEstimationViewModel.MannerEstimationStep36.UpdatePhosphorusPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep37.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep37.MannerEstimateId = mannerEstimationStep37.MannerEstimateId;
        mannerEstimationStep37.IsComingFirstTime = mannerEstimationViewModel.MannerEstimationStep34.IsComingFirstTime;
        if (!mannerEstimationViewModel.MannerEstimationStep37.IsComingFirstTime)
        {
            mannerEstimationStep37.IsComingFirstTime = true;
        }
        decimal nutrientPercentage = await FetchNutrientPrecentage((int)NMP.Commons.Enums.MannerNutrients.Phosphorus, mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId.Value);
        if (mannerEstimationViewModel.MannerEstimationStep37.UpdatePhosphorusPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice)
        {
            decimal cal1 = nutrientPercentage / 100m;
            decimal cal2 = cal1 * 1000m;
            mannerEstimationStep37.PhosphorusProductPrice =
            (int)Math.Round(cal2 * (mannerEstimationStep37.PhosphorusPrice ?? 0 / 100));
        }
        else
        {
            decimal cal1 = (nutrientPercentage / 100) * 1000;
            decimal cal2 = (mannerEstimationStep37.PhosphorusProductPrice ?? 0 / cal1);
            decimal cal3 = (cal2 / cal1) * 100;
            mannerEstimationStep37.PhosphorusPrice = Math.Round(cal3 / 100, 2);
        }

        mannerEstimationViewModel.MannerEstimationStep37 = mannerEstimationStep37;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return await GetMannerEstimationStep37();
    }
    public MannerEstimationStep38ViewModel GetMannerEstimationStep38()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep38.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        return mannerEstimationViewModel.MannerEstimationStep38;
    }
    public MannerEstimationStep38ViewModel SetMannerEstimationStep38(MannerEstimationStep38ViewModel mannerEstimationStep38)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep38 = mannerEstimationStep38;
        mannerEstimationViewModel.MannerEstimationStep38.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep38.MannerEstimateId = mannerEstimationStep38.MannerEstimateId ?? 00;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep38();
    }
    public async Task<MannerEstimationStep39ViewModel> GetMannerEstimationStep39()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep39.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep39.UpdatePotashPriceQuestion = mannerEstimationViewModel.MannerEstimationStep38.UpdatePotashPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep39.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep39.NutrientProductName = await FetchNutrientProductName((int)NMP.Commons.Enums.MannerNutrients.Potassium, mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId ?? 0);
        if (!mannerEstimationViewModel.MannerEstimationStep39.IsComingFirstTime)
        {
            mannerEstimationViewModel.MannerEstimationStep39.IsComingFirstTime = true;
            await BindNutrientPrice(mannerEstimationViewModel, (int)NMP.Commons.Enums.MannerNutrients.Potassium);
            SetMannerEstimationToSession(mannerEstimationViewModel);
        }
        return mannerEstimationViewModel.MannerEstimationStep39;
    }
    public async Task<MannerEstimationStep39ViewModel> SetMannerEstimationStep39(MannerEstimationStep39ViewModel mannerEstimationStep39)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep39.EncryptedMannerEstimateId = mannerEstimationViewModel.MannerEstimationStep35.EncryptedMannerEstimateId ?? string.Empty;
        mannerEstimationViewModel.MannerEstimationStep39.UpdatePotashPriceQuestion = mannerEstimationStep39.UpdatePotashPriceQuestion ?? mannerEstimationViewModel.MannerEstimationStep38.UpdatePotashPriceQuestion;
        mannerEstimationViewModel.MannerEstimationStep39.NutrientProductId = mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId;
        mannerEstimationViewModel.MannerEstimationStep39.MannerEstimateId = mannerEstimationStep39.MannerEstimateId;
        mannerEstimationStep39.IsComingFirstTime = mannerEstimationViewModel.MannerEstimationStep39.IsComingFirstTime;
        if (!mannerEstimationViewModel.MannerEstimationStep39.IsComingFirstTime)
        {
            mannerEstimationStep39.IsComingFirstTime = true;
        }
        decimal nutrientPercentage = await FetchNutrientPrecentage((int)NMP.Commons.Enums.MannerNutrients.Potassium, mannerEstimationViewModel.MannerEstimationStep35.NutrientProductId.Value);
        if (mannerEstimationViewModel.MannerEstimationStep39.UpdatePotashPriceQuestion == (int)NMP.Commons.Enums.UpdateNutrientPriceQuestion.UpdateByNutrientPrice)
        {
            decimal cal1 = nutrientPercentage / 100m;
            decimal cal2 = cal1 * 1000m;
            mannerEstimationStep39.PotashProductPrice =
            (int)Math.Round(cal2 * (mannerEstimationStep39.PotashPrice ?? 0 / 100));
        }
        else
        {
            decimal cal1 = (nutrientPercentage / 100) * 1000;
            decimal cal2 = (mannerEstimationStep39.PotashProductPrice ?? 0 / cal1);
            decimal cal3 = (cal2 / cal1) * 100;
            mannerEstimationStep39.PotashPrice = Math.Round(cal3 / 100, 2);
        }

        mannerEstimationViewModel.MannerEstimationStep39 = mannerEstimationStep39;
        SetMannerEstimationToSession(mannerEstimationViewModel);

        return await GetMannerEstimationStep39();
    }
    public async Task<(decimal, Error)> FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(int mannerEstimationId, DateTime startDate, DateTime endDate, bool isGreenFoodCompost, int? mannerApplicationId)
    {
        _logger.LogTrace("MannerLogic : FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost() called");
        return await _mannerEstimationService.FetchTotalNBasedByMannerEstimationIdAppDateAndIsGreenCompost(mannerEstimationId, startDate, endDate, isGreenFoodCompost, mannerApplicationId);
    }
    public async Task<(decimal, Error)> FetchTotalNByMannerEstimationIdAppDate(int mannerEstimationId, DateTime startDate, DateTime endDate, int? mannerApplicationId)
    {
        _logger.LogTrace("MannerLogic : FetchTotalNByMannerEstimationIdAppDate() called");
        return await _mannerEstimationService.FetchTotalNByMannerEstimationIdAppDate(mannerEstimationId, startDate, endDate, mannerApplicationId);
    }
    public async Task<(bool, Error)> CheckMannerGreenCompostExistanceByDateRange(int mannerEstimationId, string dateFrom, string dateTo, int? mannerApplicationId)
    {
        _logger.LogTrace("MannerLogic : CheckMannerGreenCompostExistanceByDateRange() called");
        return await _mannerEstimationService.CheckMannerGreenCompostExistanceByDateRange(mannerEstimationId, dateFrom, dateTo, mannerApplicationId);
    }

    public async Task<Error?> BindMannerEstimationDataForUpdate(int mannerEstimateId)
    {
        List<CropTypeResponse> cropTypes = await _fieldService.FetchAllCropTypesServiceAsync();
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        (MannerEstimation? mannerEstimate, Error? error) = await FetchMannerEstimateById(mannerEstimateId);
        if (mannerEstimate != null && string.IsNullOrWhiteSpace(error?.Message))
        {
            (MannerFarmViewModel? mannerFarm, error) = await FetchMannerFarmById(mannerEstimate.MannerFarmID.Value);
            mannerEstimationViewModel.MannerEstimationId = mannerEstimate.ID;
            mannerEstimationViewModel.MannerFarmId = mannerEstimate.MannerFarmID;
            mannerEstimationViewModel.MannerEstimationStep31.Name = mannerEstimate.Name;
            mannerEstimationViewModel.MannerEstimationStep1.FarmName = mannerFarm.Name;
            mannerEstimationViewModel.MannerEstimationStep2.CountryID = mannerFarm.CountryID.Value;
            mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = await FetchFarmRB209CoutryId(mannerFarm.CountryID.Value);
            mannerEstimationViewModel.MannerEstimationStep3.Postcode = mannerFarm.Postcode;
            mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall = mannerFarm.AverageAnuualRainfall.Value;
            mannerEstimationViewModel.MannerEstimationStep5.FieldName = mannerEstimate.FieldName;
            mannerEstimationViewModel.MannerEstimationStep6.IsWithinNVZ = mannerEstimate.IsWithinNVZ;
            mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic = mannerFarm.RegisteredOrganicProducer;
            mannerEstimationViewModel.MannerEstimationStep18.TopSoilId = mannerEstimate.TopSoilID;
            mannerEstimationViewModel.MannerEstimationStep19.SubSoilId = mannerEstimate.SubSoilID;
            mannerEstimationViewModel.MannerEstimationStep9.CropTypeId = mannerEstimate.CropTypeID;
            mannerEstimationViewModel.MannerEstimationStep9.MannerCropTypeId = mannerEstimate.MannerCropTypeID;
            mannerEstimationViewModel.MannerEstimationStep20.SowingDate = mannerEstimate.SowingDate;
            var cropType = cropTypes?
    .FirstOrDefault(x => x.CropTypeId == mannerEstimate?.CropTypeID);

            mannerEstimationViewModel.MannerEstimationStep8.CropGroupId =
                cropType?.CropGroupId ?? 0;

        }
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return error;

    }

    public async Task<(MannerEstimation?, Error?)> UpdateFarmFieldAndCropData(int mannerEstimationId)
    {
        (_, MannerEstimation mannerEstimation, _) = await BindMannerEstimationDataForAdd(null, mannerEstimationId);
        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerEstimation = mannerEstimation
        });

        (MannerEstimation? mannerEstimationResult, Error? error) = await _mannerEstimationService.UpdateMannerEstimationServiceAsync(jsonData);
        return (mannerEstimationResult, error);
    }

    public async Task<(MannerEstimationApplication?, Error?)> FetchMannerEstimateApplicationById(int mannerEstimateApplicationId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerEstimateApplicationById() called");
        return await _mannerEstimationService.FetchMannerEstimateApplicationByIdAsync(mannerEstimateApplicationId);
    }
    public async Task<Error?> BindApplicationDetailForUpdate(int mannerEstimateApplicationId)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        (MannerEstimationApplication? mannerEstimateApplication, Error? error) = await FetchMannerEstimateApplicationById(mannerEstimateApplicationId);
        if (mannerEstimateApplication != null && string.IsNullOrWhiteSpace(error?.Message))
        {
            mannerEstimationViewModel.MannerEstimationId = mannerEstimateApplication.MannerEstimationID;
            mannerEstimationViewModel.MannerEstimationApplicationId = mannerEstimateApplication.ID;
            mannerEstimationViewModel.MannerEstimationStep12.ManureTypeId = mannerEstimateApplication.ManureTypeID;
            mannerEstimationViewModel.MannerEstimationStep13.ApplicationDate = mannerEstimateApplication.ApplicationDate;
            mannerEstimationViewModel.MannerEstimationStep25.N = mannerEstimateApplication.N;
            mannerEstimationViewModel.MannerEstimationStep25.P2O5 = mannerEstimateApplication.P2O5;
            mannerEstimationViewModel.MannerEstimationStep25.K2O = mannerEstimateApplication.K2O;
            mannerEstimationViewModel.MannerEstimationStep25.MgO = mannerEstimateApplication.MgO;
            mannerEstimationViewModel.MannerEstimationStep25.SO3 = mannerEstimateApplication.SO3;
            mannerEstimationViewModel.MannerEstimationStep25.DryMatterPercent = mannerEstimateApplication.DryMatterPercent;
            mannerEstimationViewModel.MannerEstimationStep25.UricAcid = mannerEstimateApplication.UricAcid;
            mannerEstimationViewModel.MannerEstimationStep25.NH4N = mannerEstimateApplication.NH4N;
            mannerEstimationViewModel.MannerEstimationStep25.NO3N = mannerEstimateApplication.NO3N;

            mannerEstimationViewModel.MannerEstimationStep24.ManureType = new ManureType();
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.TotalN = mannerEstimateApplication.N;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.P2O5 = mannerEstimateApplication.P2O5;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.K2O = mannerEstimateApplication.K2O;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.MgO = mannerEstimateApplication.MgO;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.SO3 = mannerEstimateApplication.SO3;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.DryMatter = mannerEstimateApplication.DryMatterPercent;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.Uric = mannerEstimateApplication.UricAcid;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.NH4N = mannerEstimateApplication.NH4N;
            mannerEstimationViewModel.MannerEstimationStep24.ManureType.NO3N = mannerEstimateApplication.NO3N;

            mannerEstimationViewModel.MannerEstimationStep27.ApplicationRate = mannerEstimateApplication.ApplicationRate;
            mannerEstimationViewModel.MannerEstimationStep26.ApplicationRate = mannerEstimateApplication.ApplicationRate;
            mannerEstimationViewModel.MannerEstimationStep28.ApplicationRate = mannerEstimateApplication.ApplicationRate;
            mannerEstimationViewModel.MannerEstimationStep28.AreaSpread = mannerEstimateApplication.AreaSpread;
            mannerEstimationViewModel.MannerEstimationStep28.ManureQuantity = mannerEstimateApplication.ManureQuantity;
            mannerEstimationViewModel.MannerEstimationStep23.ApplicationMethodId = mannerEstimateApplication.ApplicationMethodID;
            mannerEstimationViewModel.MannerEstimationStep29.IncorporationMethodId = mannerEstimateApplication.IncorporationMethodID;
            mannerEstimationViewModel.MannerEstimationStep30.IncorporationDelayId = mannerEstimateApplication.IncorporationDelayID;
            mannerEstimationViewModel.MannerEstimationStep32.WindspeedId = mannerEstimateApplication.WindspeedID;
            mannerEstimationViewModel.MannerEstimationStep32.RainfallWithinSixHoursId = mannerEstimateApplication.RainfallWithinSixHoursID;
            mannerEstimationViewModel.MannerEstimationStep32.MoistureTypeId = mannerEstimateApplication.MoistureID;
            mannerEstimationViewModel.MannerEstimationStep32.AutumnCropNitrogenUptake = mannerEstimateApplication.AutumnCropNitrogenUptake;
            mannerEstimationViewModel.MannerEstimationStep32.SoilDrainageEndDate = mannerEstimateApplication.EndOfDrainageDate;
            mannerEstimationViewModel.MannerEstimationStep32.TotalRainfall = mannerEstimateApplication.RainfallPostApplication;
            mannerEstimationViewModel.MannerEstimationStep24.DefaultNutrientValue = await FetchDefaultNutrientValue(mannerEstimateApplication.ManureTypeID.Value, mannerEstimateApplication);
        }
        else
        {
            return error;
        }
        (ManureType? manureType, error) = await _mannerService.FetchManureTypeByManureTypeId(mannerEstimateApplication.ManureTypeID.Value);
        if (error == null && manureType != null)
        {
            mannerEstimationViewModel.MannerEstimationStep12.ManureTypeName = manureType.Name;
            (var manureGroup, error) = await _mannerService.FetchManureGroupById(manureType.ManureGroupId ?? 0);
            if (error == null && manureGroup != null)
            {
                mannerEstimationViewModel.MannerEstimationStep11.ManureGroupName = manureGroup.Name;
            }
            mannerEstimationViewModel.MannerEstimationStep11.ManureGroupId = manureType.ManureGroupId;
            if (mannerEstimateApplication.ApplicationRate == manureType.ApplicationRateArable)
            {
                mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.UseDefaultApplicationRate;
            }
            else if (mannerEstimateApplication.AreaSpread != null && mannerEstimateApplication.ManureQuantity != null)
            {
                mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.CalculateBasedOnAreaAndQuantity;
            }
            else
            {
                mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateMethod = (int)NMP.Commons.Enums.ApplicationRate.EnterAnApplicationRate;
            }
            mannerEstimationViewModel.MannerEstimationStep26.ApplicationRateArable = manureType.ApplicationRateArable;

        }
        await BindConditionAffectingNutrientValues(mannerEstimationViewModel);
        SetMannerEstimationToSession(mannerEstimationViewModel);
        await BindMannerEstimationDataForUpdate(mannerEstimateApplication.MannerEstimationID.Value);

        return error;

    }

    private async Task BindConditionAffectingNutrientValues(MannerEstimationViewModel mannerEstimationViewModel)
    {
        if (mannerEstimationViewModel.MannerEstimationStep32.MoistureTypeId != null)
        {
            (MoistureTypeResponse moistureType, _) = await _organicManureLogic
                                    .FetchMoisterTypeById(mannerEstimationViewModel.MannerEstimationStep32.MoistureTypeId.Value);
            if (moistureType != null)
            {
                mannerEstimationViewModel.MannerEstimationStep32.MoistureType = moistureType.Name;
            }
        }
        if (mannerEstimationViewModel.MannerEstimationStep32.WindspeedId != null)
        {
            (WindspeedResponse? windspeed, _) = await _organicManureLogic.FetchWindspeedById(mannerEstimationViewModel.MannerEstimationStep32.WindspeedId.Value);
            if (windspeed != null)
            {
                mannerEstimationViewModel.MannerEstimationStep32.Windspeed = windspeed.Name;
            }
        }

        if (mannerEstimationViewModel.MannerEstimationStep32.MoistureTypeId != null)
        {
            (RainTypeResponse rainType, _) = await _organicManureLogic
                    .FetchRainTypeById(mannerEstimationViewModel.MannerEstimationStep32.RainfallWithinSixHoursId.Value);
            if (rainType != null)
            {
                mannerEstimationViewModel.MannerEstimationStep32.RainfallWithinSixHours = rainType.Name;
            }
        }
    }

    public async Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimationApplicationData()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();

        MannerEstimationApplication? mannerEstimationApplication = await BindMannerEstinationApplicationData(mannerEstimationViewModel, true);

        string jsonData = JsonConvert.SerializeObject(mannerEstimationApplication);
        (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error) = await UpdateMannerEstimateApplication(jsonData);
        return (mannerEstimationApplicationResult, error);
    }

    private async Task<(MannerEstimationApplication?, Error?)> UpdateMannerEstimateApplication(string jsonData)
    {
        (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error) = await _mannerEstimationService.UpdateMannerEstimationApplicationServiceAsync(jsonData);
        return (mannerEstimationApplicationResult, error);
    }

    public async Task<int?> GetCropGroupByCropTypeId(int? cropTypeId)
    {
        List<CropTypeResponse> cropTypes = await _fieldService.FetchAllCropTypesServiceAsync();
        int? cropGroupId = cropTypes?.FirstOrDefault(x => x.CropTypeId == cropTypeId)?.CropGroupId;
        return cropGroupId;
    }
    public async Task<(MannerEstimationApplication?, Error?)> AddMannerEstimationApplication()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();

        MannerEstimationApplication? mannerEstimationApplication = await BindMannerEstinationApplicationData(mannerEstimationViewModel, false);

        string jsonData = JsonConvert.SerializeObject(mannerEstimationApplication);

        (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error) = await _mannerEstimationService.AddMannerEstimationApplicationServiceAsync(jsonData);
        return (mannerEstimationApplicationResult, error);
    }

    public MannerEstimationStep40ViewModel GetMannerEstimationStep40()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        return mannerEstimationViewModel.MannerEstimationStep40;
    }
    public MannerEstimationStep40ViewModel SetMannerEstimationStep40(MannerEstimationStep40ViewModel mannerEstimationStep40)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep40 = mannerEstimationStep40;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep40();
    }

    public async Task<Error?> RemoveMannerEstimations(string mannerEstimationIds)
    {
        Error? error = await _mannerEstimationService.RemoveMannerEstimationsServiceAsync(mannerEstimationIds);
        return error;
    }

    public MannerEstimationStep41ViewModel SetMannerEstimationStep41(MannerEstimationStep41ViewModel mannerEstimationStep41)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep41.EncryptedMannerEstimateId = mannerEstimationStep41.EncryptedMannerEstimateId;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return mannerEstimationViewModel.MannerEstimationStep41;
    }
    public MannerEstimationStep41ViewModel GetMannerEstimationStep41()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        return mannerEstimationViewModel.MannerEstimationStep41;
    }
    public async Task<(string, Error?)> DeleteMannerEstimateApplicationById(int mannerEstimationId)
    {
        _logger.LogTrace("MannerLogic : DeleteMannerEstimateApplicationById() called");
        return await _mannerEstimationService.DeleteMannerEstimateApplicationByIdServiceAsync(mannerEstimationId);

    }

    public async Task<(MannerFarmViewModel?, Error?)> FetchMannerFarmById(int mannerFarmId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerFarmById() called");
        return await _mannerEstimationService.FetchMannerFarmById(mannerFarmId);
    }

    public async Task<(List<MannerFarmViewModel>, Error?)> FetchMannerFarmListByOrgId(Guid orgId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerFarmListByOrgId() called");
        return await _mannerEstimationService.FetchMannerFarmListByOrgId(orgId);
    }
    public async Task<(List<MannerEstimationSummaryViewModel>, Error?)> FetchMannerEstimateByFarmId(int mannerFarmId)
    {
        _logger.LogTrace("MannerEstimationLogic : FetchMannerEstimateByFarmId() called");
        return await _mannerEstimationService.FetchMannerEstimateByFarmIdAsync(mannerFarmId);
    }

    public async Task<(MannerEstimationApplication?, Error?)> AddNewMannerEstimation()
    {
        (MannerEstimationViewModel mannerEstimationViewModel, MannerEstimation mannerEstimate, _) = await BindMannerEstimationDataForAdd(null, null);

        MannerEstimationApplication? mannerEstimationApplication = await BindMannerEstinationApplicationData(mannerEstimationViewModel, false);

        string jsonData = JsonConvert.SerializeObject(new
        {
            MannerEstimation = mannerEstimate,
            MannerEstimationApplication = mannerEstimationApplication
        });

        (MannerEstimationApplication? mannerEstimationApplicationResult, Error? error) = await _mannerEstimationService.AddMannerEstimationServiceAsync(jsonData);
        return (mannerEstimationApplicationResult, error);
    }
    public bool CheckSandyShallowByTopSoilSubSoilId(int topSoilId, int subSoilId, int countryId)
    {
        _logger.LogTrace("MannerEstimationLogic : CheckSandyShallowByTopSoilSubSoilId() called");

        var topSoil = (TopSoil)topSoilId;
        var subSoil = (SubSoil)subSoilId;

        // "Any" topsoil combinations - shallow soils regardless of topsoil type
        if (subSoil == SubSoil.Chalk || subSoil == SubSoil.Rock)
        {
            return true;
        }

        (TopSoil Top, SubSoil Sub)[] sandyShallowCombinations;

        switch (countryId)
        {
            case 2:
                sandyShallowCombinations = new (TopSoil Top, SubSoil Sub)[]
                {
                (TopSoil.Sand, SubSoil.Sand),
                (TopSoil.Sand, SubSoil.LoamySand),
                (TopSoil.Sand, SubSoil.SandyLoam),
                (TopSoil.LoamySand, SubSoil.Sand),
                (TopSoil.LoamySand, SubSoil.LoamySand),
                (TopSoil.LoamySand, SubSoil.SandyLoam),
                (TopSoil.SandyLoam, SubSoil.Sand),
                (TopSoil.SandyLoam, SubSoil.LoamySand),
                (TopSoil.SandyLoam, SubSoil.SandyLoam),
                };
                break;

            default:
                sandyShallowCombinations = new (TopSoil Top, SubSoil Sub)[]
                {
                (TopSoil.Sand, SubSoil.Sand),
                (TopSoil.Sand, SubSoil.LoamySand),
                (TopSoil.LoamySand, SubSoil.Sand),
                (TopSoil.LoamySand, SubSoil.LoamySand),
                (TopSoil.SandyLoam, SubSoil.Sand),
                (TopSoil.SandyLoam, SubSoil.LoamySand),
                };
                break;
        }

        return sandyShallowCombinations.Any(c => c.Top == topSoil && c.Sub == subSoil);
    }

    public async Task BindFarmDataForMannerEstimateUpdateOrCreate(int mannerFarmId)
    {
        (MannerFarmViewModel? mannerFarm, _) = await FetchMannerFarmById(mannerFarmId);
        if (mannerFarm != null)
        {
            MannerEstimationViewModel? mannerEstimationViewModel = GetMannerEstimationFromSession();
            if (mannerEstimationViewModel != null)
            {
                _httpContextAccessor.HttpContext?.Session.SetString("current_manner_estimate_farm_name", mannerFarm.Name);
                mannerEstimationViewModel.MannerEstimationStep2.CountryID = mannerFarm.CountryID ?? 0;
                mannerEstimationViewModel.MannerEstimationStep2.FarmRB209CountryId = await FetchFarmRB209CoutryId(mannerFarm.CountryID ?? 0);
                mannerEstimationViewModel.MannerEstimationStep3.Postcode = mannerFarm.Postcode;
                mannerEstimationViewModel.MannerEstimationStep4.AverageAnnualRainfall = mannerFarm.AverageAnuualRainfall ?? 0;
                mannerEstimationViewModel.MannerEstimationStep17.IsFarmOrganic = mannerFarm.RegisteredOrganicProducer;
            }
            SetMannerEstimationToSession(mannerEstimationViewModel);
        }
    }
    public MannerEstimationStep42ViewModel GetMannerEstimationStep42()
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        return mannerEstimationViewModel.MannerEstimationStep42;
    }
    public MannerEstimationStep42ViewModel SetMannerEstimationStep42(MannerEstimationStep42ViewModel mannerEstimationStep42)
    {
        MannerEstimationViewModel mannerEstimationViewModel = GetMannerEstimation();
        mannerEstimationViewModel.MannerEstimationStep42 = mannerEstimationStep42;
        SetMannerEstimationToSession(mannerEstimationViewModel);
        return GetMannerEstimationStep42();
    }

    public async Task<Error?> RemoveMannerFarms(string mannerFarmIds)
    {
        Error? error = await _mannerEstimationService.RemoveMannerFarmsServiceAsync(mannerFarmIds);
        return error;
    }
    public async Task<bool> FetchIsExistMannerFarmByOrgIdAndName(Guid organisationId, string farmName)
    {
        _logger.LogTrace("ManureLogic : FetchIsExistMannerFarmByOrgIdAndName() called");
        return await _mannerEstimationService.FetchIsExistMannerFarmByOrgIdAndNameAsyncAPI(organisationId, farmName);
    }

}

