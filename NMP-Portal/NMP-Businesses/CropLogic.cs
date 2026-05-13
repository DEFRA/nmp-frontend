using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class CropLogic(ILogger<CropLogic> logger, ICropService cropService, IDataProtectionProvider dataProtectionProvider, IFieldLogic fieldLogic, ISnsAnalysisService snsAnalysisService, IRecommendationService recommendationService, IPreviousCroppingLogic previousCroppingLogic) : ICropLogic
{
    private readonly ILogger<CropLogic> _logger = logger;
    private readonly ICropService _cropService = cropService;
    private readonly ISnsAnalysisService _snsAnalysisService = snsAnalysisService;
    private readonly IRecommendationService _recommendationService = recommendationService;
    private readonly IPreviousCroppingLogic _previousCroppingLogic = previousCroppingLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly IDataProtector _farmDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FarmController");
    private readonly IDataProtector _fieldDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.FieldController");
    private readonly IDataProtector _cropDataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.CropController");
    public async Task<(bool, Error?)> AddCropNutrientManagementPlan(CropDataWrapper cropData)
    {
        _logger.LogTrace("Adding crop nutrient management plan");
        return await _cropService.AddCropNutrientManagementPlanServiceAsync(cropData);
    }

    public async Task<(bool, Error)> CopyCropNutrientManagementPlan(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser)
    {
        _logger.LogTrace("Copying crop nutrient management plan for FarmID: {FarmID}, HarvestYear: {HarvestYear}, CopyYear: {CopyYear}", farmID, harvestYear, copyYear);
        return await _cropService.CopyCropNutrientManagementPlanServiceAsync(farmID, harvestYear, copyYear, isOrganic, isFertiliser);
    }

    public async Task<(Crop?, Error?)> FetchCropById(int id)
    {
        _logger.LogTrace("Fetching crop by ID: {CropId}", id);
        return await _cropService.FetchCropByIdServiceAsync(id);
    }

    public async Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(int cropTypeId, int cropInfo1Id)
    {
        _logger.LogTrace("Fetching CropInfo1 name for CropTypeId: {CropTypeId}, CropInfo1Id: {CropInfo1Id}", cropTypeId, cropInfo1Id);
        return await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1IdServiceAsync(cropTypeId, cropInfo1Id);
    }

    public async Task<string> FetchCropInfo2NameByCropInfo2Id(int cropInfo2Id)
    {
        _logger.LogTrace("Fetching CropInfo2 name for CropInfo2Id: {CropInfo2Id}", cropInfo2Id);
        return await _cropService.FetchCropInfo2NameByCropInfo2IdServiceAsync(cropInfo2Id);
    }

    public async Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId, int? farmRB209CountryID)
    {
        _logger.LogTrace("Fetching CropInfoOne for CropTypeId: {CropTypeId}", cropTypeId);
        List<CropInfoOneResponse> cropInfoOneResponse = await _cropService.FetchCropInfoOneByCropTypeIdServiceAsync(cropTypeId);
        if (farmRB209CountryID.HasValue)
        {
            cropInfoOneResponse = cropInfoOneResponse.Where(x => x.CountryId == farmRB209CountryID || x.CountryId == (int)NMP.Commons.Enums.RB209Country.All).ToList();
        }
        return cropInfoOneResponse;
    }

    public async Task<string?> FetchCropInfoOneQuestionByCropTypeId(int cropTypeId, int countryId)
    {
        _logger.LogTrace("Fetching CropInfoOne question for CropTypeId: {CropTypeId}", cropTypeId);
        return await _cropService.FetchCropInfoOneQuestionByCropTypeIdServiceAsync(cropTypeId, countryId);
    }

    public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeId()
    {
        _logger.LogTrace("Fetching CropInfoTwo");
        return await _cropService.FetchCropInfoTwoByCropTypeIdServiceAsync();
    }

    public async Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYear(int fieldId, int year)
    {
        _logger.LogTrace("Fetching crop plan for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _cropService.FetchCropPlanByFieldIdAndYearServiceAsync(fieldId, year);
    }

    public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetching crops for FieldId: {FieldId}", fieldId);
        return await _cropService.FetchCropsByFieldIdServiceAsync(fieldId);
    }

    public async Task<int> FetchCropTypeByGroupId(int cropGroupId)
    {
        _logger.LogTrace("Fetching crop type by CropGroupId: {CropGroupId}", cropGroupId);
        return await _cropService.FetchCropTypeByGroupIdServiceAsync(cropGroupId);
    }

    public async Task<decimal> FetchCropTypeDefaultYieldByCropTypeId(int cropTypeId, bool isScotland)
    {
        _logger.LogTrace("Fetching default yield for CropTypeId: {CropTypeId}", cropTypeId);
        return await _cropService.FetchCropTypeDefaultYieldByCropTypeIdServiceAsync(cropTypeId, isScotland);
    }

    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinking()
    {
        _logger.LogTrace("Fetching crop type linking");
        return await _cropService.FetchCropTypeLinkingServiceAsync();
    }

    public async Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesById(int defoliationId)
    {
        _logger.LogTrace("Fetching defoliation sequence by ID: {DefoliationId}", defoliationId);
        return await _cropService.FetchDefoliationSequencesByIdServiceAsync(defoliationId);
    }

    public async Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(int swardTypeId, int swardManagementId, int numberOfCut, bool isNewSward)
    {
        _logger.LogTrace("Fetching defoliation sequences for SwardTypeId: {SwardTypeId}, SwardManagementId: {SwardManagementId}, NumberOfCut: {NumberOfCut}, IsNewSward: {IsNewSward}", swardTypeId, swardManagementId, numberOfCut, isNewSward);
        return await _cropService.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCutServiceAsync(swardTypeId, swardManagementId, numberOfCut, isNewSward);
    }

    public async Task<(List<GrassGrowthClassResponse>, Error?)> FetchGrassGrowthClass(List<int> fieldIds)
    {
        _logger.LogTrace("Fetching grass growth class for FieldIds: {FieldIds}", string.Join(", ", fieldIds));
        return await _cropService.FetchGrassGrowthClassServiceAsync(fieldIds);
    }

    public async Task<List<GrassSeasonResponse>> FetchGrassSeasons()
    {
        _logger.LogTrace("Fetching grass seasons");
        return await _cropService.FetchGrassSeasonsServiceAsync();
    }

    public async Task<(List<HarvestYearPlanResponse>, Error?)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId)
    {
        _logger.LogTrace("Fetching harvest year plans for FarmId: {FarmId}, HarvestYear: {HarvestYear}", farmId, harvestYear);
        return await _cropService.FetchHarvestYearPlansByFarmIdServiceAsync(harvestYear, farmId);
    }

    public async Task<(HarvestYearResponseHeader?, Error?)> FetchHarvestYearPlansDetailsByFarmId(int harvestYear, int farmId)
    {
        _logger.LogTrace("Fetching harvest year plan details for FarmId: {FarmId}, HarvestYear: {HarvestYear}", farmId, harvestYear);
        return await _cropService.FetchHarvestYearPlansDetailsByFarmIdServiceAsync(harvestYear, farmId);
    }

    public async Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropId(int cropId, bool isShortSummary)
    {
        _logger.LogTrace("Fetching management periods for CropId: {CropId}, IsShortSummary: {IsShortSummary}", cropId, isShortSummary);
        return await _cropService.FetchManagementperiodByCropIdServiceAsync(cropId, isShortSummary);
    }

    public async Task<(ManagementPeriod?, Error?)> FetchManagementperiodById(int id)
    {
        _logger.LogTrace("Fetching management period by ID: {Id}", id);
        return await _cropService.FetchManagementperiodByIdServiceAsync(id);
    }

    public async Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId, int type)
    {
        _logger.LogTrace("Fetching plan summary for FarmId: {FarmId}, Type: {Type}", farmId, type);
        return await _cropService.FetchPlanSummaryByFarmIdServiceAsync(farmId, type);
    }

    public async Task<List<PotatoVarietyResponse>> FetchPotatoVarieties()
    {
        _logger.LogTrace("Fetching potato varieties");
        return await _cropService.FetchPotatoVarietiesServiceAsync();
    }

    public async Task<(List<PotentialCutResponse>, Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementId(int swardTypeId, int swardManagementId)
    {
        _logger.LogTrace("Fetching potential cuts for SwardTypeId: {SwardTypeId}, SwardManagementId: {SwardManagementId}", swardTypeId, swardManagementId);
        return await _cropService.FetchPotentialCutsBySwardTypeIdAndSwardManagementIdServiceAsync(swardTypeId, swardManagementId);
    }

    public async Task<(List<RecommendationHeader>, Error?)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear)
    {
        _logger.LogTrace("Fetching recommendations for FieldId: {FieldId}, HarvestYear: {HarvestYear}", fieldId, harvestYear);
        return await _cropService.FetchRecommendationByFieldIdAndYearServiceAsync(fieldId, harvestYear);
    }

    public async Task<List<int>> FetchSecondCropListByFirstCropId(int firstCropTypeId, int rb209CountryId)
    {
        _logger.LogTrace("Fetching second crop list for FirstCropTypeId: {0},Rb209CountryId: {1}", firstCropTypeId, rb209CountryId);
        return await _cropService.FetchSecondCropListByFirstCropIdServiceAsync(firstCropTypeId, rb209CountryId);
    }

    public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementId(int swardManagementId)
    {
        _logger.LogTrace("Fetching sward management by ID: {SwardManagementId}", swardManagementId);
        return await _cropService.FetchSwardManagementBySwardManagementIdServiceAsync(swardManagementId);
    }

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeId(int swardTypeId)
    {
        _logger.LogTrace("Fetching sward managements for SwardTypeId: {SwardTypeId}", swardTypeId);
        return await _cropService.FetchSwardManagementBySwardTypeIdServiceAsync(swardTypeId);
    }

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagements()
    {
        _logger.LogTrace("Fetching sward managements");
        return await _cropService.FetchSwardManagementsServiceAsync();
    }

    public async Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeId(int swardTypeId)
    {
        _logger.LogTrace("Fetching sward type by ID: {SwardTypeId}", swardTypeId);
        return await _cropService.FetchSwardTypeBySwardTypeIdServiceAsync(swardTypeId);
    }

    public async Task<(List<SwardTypeResponse>, Error)> FetchSwardTypes()
    {
        _logger.LogTrace("Fetching sward types");
        return await _cropService.FetchSwardTypesServiceAsync();
    }

    public async Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(int sequenceId, int grassGrowthClassId)
    {
        _logger.LogTrace("Fetching yield ranges for SequenceId: {SequenceId}, GrassGrowthClassId: {GrassGrowthClassId}", sequenceId, grassGrowthClassId);
        return await _cropService.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassIdServiceAsync(sequenceId, grassGrowthClassId);
    }

    public async Task<(bool, Error?)> IsCropsGroupNameExistForUpdate(string cropIds, string cropGroupName, int year, int farmId)
    {
        _logger.LogTrace("Checking if crop group name exists for update: {CropGroupName} in FarmId: {FarmId}, Year: {Year}", cropGroupName, farmId, year);
        return await _cropService.IsCropsGroupNameExistForUpdateServiceAsync(cropIds, cropGroupName, year, farmId);
    }

    public async Task<(bool, Error)> MergeCrop(string cropData)
    {
        _logger.LogTrace("Merging crop data");
        return await _cropService.MergeCropServiceAsync(cropData);
    }

    public async Task<(string, Error?)> RemoveCropPlan(List<int> cropIds)
    {
        _logger.LogTrace("Removing crop plans for CropIds: {CropIds}", string.Join(", ", cropIds));
        return await _cropService.RemoveCropPlanServiceAsync(cropIds);
    }

    public async Task<(List<Crop>, Error)> UpdateCrop(string cropData)
    {
        _logger.LogTrace("Updating crop data");
        return await _cropService.UpdateCropServiceAsync(cropData);
    }

    public async Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId)
    {
        _logger.LogTrace("SnsAnalysisLogic : FetchSnsAnalysisByCropIdAsync() called");
        return await _snsAnalysisService.FetchSnsAnalysisByCropIdAsync(cropId);
    }
    public async Task<bool> FetchIsPerennialByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("CropLogic : FetchIsPerennialByCropTypeId() called");
        return await _cropService.FetchIsPerennialByCropTypeIdServiceAsync(cropTypeId);
    }
    public async Task<(Recommendation?, Error?)> FetchRecommendationByManagementPeriodId(int managementPeriodID)
    {
        _logger.LogTrace("CropLogic : Fetch Recommendation By ManagementPeriodId:{0} called", managementPeriodID);
        return await _recommendationService.FetchRecommendationByManagementPeriodId(managementPeriodID);
    }
    public async Task<(List<PreviousCroppingData>?, Error?)> FetchDataByFieldId(int fieldId, int year)
    {
        _logger.LogTrace("CropLogic : Fetch PreviousCropping By FieldId:{0} and Year:{1} called", fieldId, year);
        return await _previousCroppingLogic.FetchDataByFieldId(fieldId, year);
    }



    public async Task<(RecommendationViewModel, string)> BindDataForRecommendation(string q, string? s, RecommendationViewModel model, Error? error, List<RecommendationHeader> recommendations, string firstCropName)
    {
        CreateNewObject(model);

        int cropCounter = 0;

        firstCropName = recommendations[0]?.Crops?.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass ? NMP.Commons.Enums.CropTypes.GetName(typeof(CropTypes), recommendations[0].Crops.CropTypeID) : await _fieldLogic.FetchCropTypeById(recommendations[0].Crops.CropTypeID.Value);
        foreach (var recommendation in recommendations)
        {
            string cropTypeName = recommendation.Crops.CropTypeID == (int)CropTypes.Grass
     ? nameof(CropTypes.Grass)
     : await _fieldLogic.FetchCropTypeById(recommendation.Crops.CropTypeID!.Value);
            //check sns already exist or not in SnsAnalyses table by cropID
            SnsAnalysis snsData = await FetchSnsAnalysisByCropIdAsync(recommendation.Crops.ID ?? 0);
            var crop = new CropViewModel
            {
                ID = recommendation.Crops.ID,
                EncryptedCropId = _cropDataProtector.Protect(recommendation.Crops.ID.ToString()),
                Year = recommendation.Crops.Year,
                CropTypeID = recommendation.Crops.CropTypeID,
                FieldID = recommendation.Crops.FieldID,
                EncryptedFieldId = _fieldDataProtector.Protect(recommendation.Crops.FieldID.ToString()),
                Variety = recommendation.Crops.Variety,
                CropInfo1 = recommendation.Crops.CropInfo1,
                CropInfo2 = recommendation.Crops.CropInfo2,
                Yield = recommendation.Crops.Yield,
                SowingDate = recommendation.Crops.SowingDate,
                OtherCropName = recommendation.Crops.OtherCropName,
                CropTypeName = cropTypeName,
                IsSnsExist = snsData.CropID > 0,
                SnsAnalysisData = snsData,
                SwardManagementName = recommendation.Crops.SwardManagementName,
                EstablishmentName = recommendation.Crops.EstablishmentName,
                SwardTypeName = recommendation.Crops.SwardTypeName,
                DefoliationSequenceName = recommendation.Crops.DefoliationSequenceName,
                CropGroupName = recommendation.Crops.CropGroupName,
                SwardManagementID = recommendation.Crops.SwardManagementID,
                Establishment = recommendation.Crops.Establishment,
                SwardTypeID = recommendation.Crops.SwardTypeID,
                DefoliationSequenceID = recommendation.Crops.DefoliationSequenceID,
                PotentialCut = recommendation.Crops.PotentialCut
            };
            cropCounter++;

            crop = await BindEncryptedValueForRecommendation(recommendation, crop, model);

            model.FieldName = (await _fieldLogic.FetchFieldByFieldId(recommendation.Crops.FieldID.Value)).Name;



            model.Crops.Add(crop);
            model = BindPkBalanceForRecommendation(model, recommendation);

        }

        model = await BindNutrientListForRecommendation(model);

        return (model, firstCropName);
    }

    private async Task<CropViewModel> BindEncryptedValueForRecommendation(RecommendationHeader recommendation, CropViewModel crop, RecommendationViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(crop.CropTypeName))
        {
            crop.EncryptedCropTypeName = _cropDataProtector.Protect(crop.CropTypeName);
        }

        if (!string.IsNullOrWhiteSpace(crop.CropGroupName))
        {
            crop.EncryptedCropGroupName = _cropDataProtector.Protect(crop.CropGroupName);
        }

        if (!string.IsNullOrWhiteSpace(recommendation.Crops.CropOrder.ToString()))
        {
            crop.EncryptedCropOrder = _cropDataProtector.Protect(recommendation.Crops.CropOrder.ToString());
        }

        if (recommendation.Crops.CropInfo1 != null)
        {
            crop.CropInfo1Name = await FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(recommendation.Crops.CropTypeID.Value, recommendation.Crops.CropInfo1.Value);
        }
        if (!string.IsNullOrWhiteSpace(model.FieldName))
        {
            crop.EncryptedFieldName = _cropDataProtector.Protect(model.FieldName);
        }


        List<CropTypeResponse> cropTypeResponseList = (await _fieldLogic.FetchAllCropTypes());
        if (cropTypeResponseList != null)
        {
            CropTypeResponse cropTypeResponse = cropTypeResponseList.First(x => x.CropTypeId == crop.CropTypeID);
            if (cropTypeResponse != null)
            {
                crop.CropGroupID = cropTypeResponse.CropGroupId;
            }
        }

        if (recommendation.Crops.CropInfo2 != null && crop.CropGroupID == (int)NMP.Commons.Enums.CropGroup.Cereals)
        {
            crop.CropInfo2Name = await FetchCropInfo2NameByCropInfo2Id(crop.CropInfo2.Value);
        }
        return crop;
    }

    private async Task<RecommendationViewModel> BindNutrientListForRecommendation(RecommendationViewModel model)
    {
        (List<NutrientResponseWrapper> nutrients, _) = await _fieldLogic.FetchNutrientsAsync();
        if (nutrients.Count > 0)
        {
            model.Nutrients = new List<NutrientResponseWrapper>();
            model.Nutrients = nutrients;
        }

        return model;
    }

    private static RecommendationViewModel BindPkBalanceForRecommendation(RecommendationViewModel model, RecommendationHeader recommendation)
    {
        if (recommendation.PKBalance != null)
        {
            model.PKBalance = new PKBalance();
            model.PKBalance.PBalance = recommendation.PKBalance.PBalance;
            model.PKBalance.KBalance = recommendation.PKBalance.KBalance;
        }
        return model;
    }

    private static void CreateNewObject(RecommendationViewModel model)
    {
        if (model.Crops == null)
        {
            model.Crops = new List<CropViewModel>();
        }

        if (model.ManagementPeriods == null)
        {
            model.ManagementPeriods = new List<ManagementPeriodViewModel>();
        }

        if (model.Recommendations == null)
        {
            model.Recommendations = new List<Recommendation>();
        }

        if (model.RecommendationComments == null)
        {
            model.RecommendationComments = new List<RecommendationComment>();
        }

        if (model.OrganicManures == null)
        {
            model.OrganicManures = new List<OrganicManureDataViewModel>();
        }

        if (model.FertiliserManures == null)
        {
            model.FertiliserManures = new List<FertiliserManureDataViewModel>();
        }
    }
    public RecommendationViewModel BindRecommendationCommentForRecommendation(RecommendationViewModel model, RecommendationData recData)
    {
        foreach (var item in recData.RecommendationComments)
        {
            var recCom = new RecommendationComment
            {
                ID = item.ID,
                RecommendationID = item.RecommendationID,
                Nutrient = item.Nutrient,
                Comment = item.Comment
            };
            model.RecommendationComments.Add(recCom);
        }
        return model;
    }
    public RecommendationViewModel BindOrganicManureDataForRecommendation(RecommendationViewModel model, OrganicManureDataViewModel item, ManureType manureType)
    {
        var orgManure = new OrganicManureDataViewModel
        {
            ID = item.ID,
            ManureTypeName = item.ManureTypeName,
            ApplicationMethodName = item.ApplicationMethodName,
            ApplicationDate = item.ApplicationDate,
            ApplicationRate = item.ApplicationRate,
            EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
            EncryptedFieldName = _cropDataProtector.Protect(model.FieldName),
            EncryptedManureTypeName = _cropDataProtector.Protect(item.ManureTypeName),
            RateUnit = manureType.IsLiquid.Value ? Resource.lblCubicMeters : Resource.lbltonnes
        };

        model.OrganicManures.Add(orgManure);
        return model;
    }
    public RecommendationViewModel BindFertiliserDataForRecommendation(RecommendationViewModel model, RecommendationData recData)
    {
        foreach (var item in recData.FertiliserManures)
        {
            var fertiliserManure = new FertiliserManureDataViewModel
            {
                ID = item.ID,
                ManagementPeriodID = item.ManagementPeriodID,
                ApplicationDate = item.ApplicationDate,
                ApplicationRate = item.ApplicationRate,
                Confirm = item.Confirm,
                N = item.N,
                P2O5 = item.P2O5,
                K2O = item.K2O,
                MgO = item.MgO,
                SO3 = item.SO3,
                Na2O = item.Na2O,
                Lime = item.Lime,
                NH4N = item.NH4N,
                NO3N = item.NO3N,
                EncryptedId = _cropDataProtector.Protect(item.ID.ToString()),
                EncryptedFieldName = _cropDataProtector.Protect(model.FieldName)
            };


            model.FertiliserManures.Add(fertiliserManure);
        }
        return model;
    }
    public RecommendationViewModel BindManagementPeriodForRecommendation(RecommendationViewModel model, RecommendationData recData, string defoliationSequenceName)
    {
        var ManagementPeriods = new ManagementPeriodViewModel
        {
            ID = recData.ManagementPeriod.ID,
            CropID = recData.ManagementPeriod.CropID,
            Defoliation = recData.ManagementPeriod.Defoliation,
            DefoliationSequenceName = defoliationSequenceName,
            Utilisation1ID = recData.ManagementPeriod.Utilisation1ID,
            Utilisation2ID = recData.ManagementPeriod.Utilisation2ID,
            PloughedDown = recData.ManagementPeriod.PloughedDown
        };
        model.ManagementPeriods.Add(ManagementPeriods);
        return model;
    }
    public string BindDefoliationSequenceNameForRecommendation(string[]? defolicationParts, int defIndex)
    {
        string part = (defolicationParts != null && defIndex < defolicationParts.Length) ? defolicationParts[defIndex].Trim() : string.Empty;
        string defoliationSequenceName = (!string.IsNullOrWhiteSpace(part)) ? char.ToUpper(part[0]).ToString() + part.Substring(1) : string.Empty;
        return defoliationSequenceName;
    }
    public async Task<string> BindDefoliationNameForRecommendation(RecommendationHeader recommendation, CropViewModel crop)
    {
        string defolicationName = string.Empty;
        if (recommendation.Crops.SwardTypeID != null && recommendation.Crops.PotentialCut != null && recommendation.Crops.DefoliationSequenceID != null
            && (string.IsNullOrWhiteSpace(defolicationName) && recommendation.Crops.CropTypeID == (int)NMP.Commons.Enums.CropTypes.Grass))
        {
            (DefoliationSequenceResponse defoliationSequence, _) = await FetchDefoliationSequencesById(crop.DefoliationSequenceID.Value);
            if (!string.IsNullOrWhiteSpace(defoliationSequence.DefoliationSequenceDescription))
            {
                defolicationName = defoliationSequence.DefoliationSequenceDescription;
            }
        }

        return defolicationName;
    }
    public PlanViewModel FilterOrganicAndInorganicListForHarvestYearOverview(PlanViewModel model, string? s, string? u, string? t)
    {
        string decrypSortBy = _cropDataProtector.Unprotect(s);
        string decrypOrder = _cropDataProtector.Unprotect(u);
        if (!string.IsNullOrWhiteSpace(decrypSortBy) && !string.IsNullOrWhiteSpace(decrypOrder) && !string.IsNullOrWhiteSpace(t))
        {
            string decryptTabName = _cropDataProtector.Unprotect(t);
            model = ApplyApplicationListSorting(model, decryptTabName, decrypSortBy, decrypOrder);
        }
        return model;
    }
    private PlanViewModel ApplyApplicationListSorting(
    PlanViewModel model,
    string decryptTabName,
    string decrypSortBy,
    string decrypOrder)
    {
        bool isDescending = decrypOrder == Resource.lblDesc;

        if (decryptTabName == Resource.lblOrganicMaterialApplicationsForSorting &&
            model.HarvestYearPlans.OrganicManureList != null)
        {
            model.HarvestYearPlans.OrganicManureList =
                SortApplicationList(
                    model.HarvestYearPlans.OrganicManureList,
                    decrypSortBy,
                    isDescending);

            UpdateSortState(model, decrypSortBy, isDescending, true);
        }
        else if (decryptTabName == Resource.lblInorganicFertiliserApplicationsForSorting &&
                 model.HarvestYearPlans.InorganicFertiliserList != null)
        {
            model.HarvestYearPlans.InorganicFertiliserList =
                SortApplicationList(
                    model.HarvestYearPlans.InorganicFertiliserList,
                    decrypSortBy,
                    isDescending);

            UpdateSortState(model, decrypSortBy, isDescending, false);
        }
        return model;
    }

    private List<T> SortApplicationList<T>(
     List<T> list,
     string sortBy,
     bool isDescending)
    {
        if (sortBy == Resource.lblField)
        {
            return isDescending
                ? list.OrderByDescending(x => GetPropertyValue<string>(x, "Field")).ToList()
                : list.OrderBy(x => GetPropertyValue<string>(x, "Field")).ToList();
        }

        if (sortBy == Resource.lblDate)
        {
            return isDescending
                ? list.OrderByDescending(x => GetPropertyValue<DateTime>(x, "ApplicationDate")).ToList()
                : list.OrderBy(x => GetPropertyValue<DateTime>(x, "ApplicationDate")).ToList();
        }

        if (sortBy == Resource.lblCropType)
        {
            return isDescending
                ? list.OrderByDescending(x => GetPropertyValue<string>(x, "Crop")).ToList()
                : list.OrderBy(x => GetPropertyValue<string>(x, "Crop")).ToList();
        }

        return list;
    }

    private TValue GetPropertyValue<TValue>(object obj, string propertyName)
    {
        return (TValue)obj.GetType().GetProperty(propertyName)?.GetValue(obj)!;
    }

    private void UpdateSortState(
        PlanViewModel model,
        string sortBy,
        bool isDescending,
        bool isOrganic)
    {
        string order = isDescending ? Resource.lblDesc : Resource.lblAsc;
        string encryptedOrder = _cropDataProtector.Protect(order);

        if (isOrganic)
        {
            model.SortOrganicListOrderByFieldName = null;
            model.SortOrganicListOrderByDate = null;
            model.SortOrganicListOrderByCropType = null;

            switch (sortBy)
            {
                case var s when s == Resource.lblField:
                    model.SortOrganicListOrderByFieldName = order;
                    model.EncryptSortOrganicListOrderByFieldName = encryptedOrder;
                    break;

                case var s when s == Resource.lblDate:
                    model.SortOrganicListOrderByDate = order;
                    model.EncryptSortOrganicListOrderByDate = encryptedOrder;
                    break;

                case var s when s == Resource.lblCropType:
                    model.SortOrganicListOrderByCropType = order;
                    model.EncryptSortOrganicListOrderByCropType = encryptedOrder;
                    break;
            }
        }
        else
        {
            model.SortInOrganicListOrderByFieldName = null;
            model.SortInOrganicListOrderByDate = null;
            model.SortInOrganicListOrderByCropType = null;

            switch (sortBy)
            {
                case var s when s == Resource.lblField:
                    model.SortInOrganicListOrderByFieldName = order;
                    model.EncryptSortInOrganicListOrderByFieldName = encryptedOrder;
                    break;

                case var s when s == Resource.lblDate:
                    model.SortInOrganicListOrderByDate = order;
                    model.EncryptSortInOrganicListOrderByDate = encryptedOrder;
                    break;

                case var s when s == Resource.lblCropType:
                    model.SortInOrganicListOrderByCropType = order;
                    model.EncryptSortInOrganicListOrderByCropType = encryptedOrder;
                    break;
            }
        }
    }

}
