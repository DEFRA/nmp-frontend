using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class CropLogic(ILogger<CropLogic> logger, ICropService cropService, ISnsAnalysisService snsAnalysisService) : ICropLogic
{
    private readonly ILogger<CropLogic> _logger = logger;
    private readonly ICropService _cropService = cropService;
    private readonly ISnsAnalysisService _snsAnalysisService = snsAnalysisService;
    public async Task<(bool, Error?)> AddCropNutrientManagementPlan(CropDataWrapper cropData)
    {
        _logger.LogTrace("Adding crop nutrient management plan");
        return await _cropService.AddCropNutrientManagementPlan(cropData);
    }

    public async Task<(bool, Error)> CopyCropNutrientManagementPlan(int farmID, int harvestYear, int copyYear, bool isOrganic, bool isFertiliser)
    {
        _logger.LogTrace("Copying crop nutrient management plan for FarmID: {FarmID}, HarvestYear: {HarvestYear}, CopyYear: {CopyYear}", farmID, harvestYear, copyYear);
        return await _cropService.CopyCropNutrientManagementPlan(farmID, harvestYear, copyYear, isOrganic, isFertiliser);
    }

    public async Task<(Crop, Error)> FetchCropById(int id)
    {
        _logger.LogTrace("Fetching crop by ID: {CropId}", id);
        return await _cropService.FetchCropById(id);
    }

    public async Task<string> FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(int cropTypeId, int cropInfo1Id)
    {
        _logger.LogTrace("Fetching CropInfo1 name for CropTypeId: {CropTypeId}, CropInfo1Id: {CropInfo1Id}", cropTypeId, cropInfo1Id);
        return await _cropService.FetchCropInfo1NameByCropTypeIdAndCropInfo1Id(cropTypeId, cropInfo1Id);
    }

    public async Task<string> FetchCropInfo2NameByCropInfo2Id(int cropInfo2Id)
    {
        _logger.LogTrace("Fetching CropInfo2 name for CropInfo2Id: {CropInfo2Id}", cropInfo2Id);
        return await _cropService.FetchCropInfo2NameByCropInfo2Id(cropInfo2Id);
    }

    public async Task<List<CropInfoOneResponse>> FetchCropInfoOneByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching CropInfoOne for CropTypeId: {CropTypeId}", cropTypeId);
        return await _cropService.FetchCropInfoOneByCropTypeId(cropTypeId);
    }

    public async Task<string?> FetchCropInfoOneQuestionByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching CropInfoOne question for CropTypeId: {CropTypeId}", cropTypeId);
        return await _cropService.FetchCropInfoOneQuestionByCropTypeId(cropTypeId);
    }

    public async Task<List<CropInfoTwoResponse>> FetchCropInfoTwoByCropTypeId()
    {
        _logger.LogTrace("Fetching CropInfoTwo");
        return await _cropService.FetchCropInfoTwoByCropTypeId();
    }

    public async Task<(List<Crop>, Error)> FetchCropPlanByFieldIdAndYear(int fieldId, int year)
    {
        _logger.LogTrace("Fetching crop plan for FieldId: {FieldId}, Year: {Year}", fieldId, year);
        return await _cropService.FetchCropPlanByFieldIdAndYear(fieldId, year);
    }

    public async Task<List<Crop>> FetchCropsByFieldId(int fieldId)
    {
        _logger.LogTrace("Fetching crops for FieldId: {FieldId}", fieldId);
        return await _cropService.FetchCropsByFieldId(fieldId);
    }

    public async Task<int> FetchCropTypeByGroupId(int cropGroupId)
    {
        _logger.LogTrace("Fetching crop type by CropGroupId: {CropGroupId}", cropGroupId);
        return await _cropService.FetchCropTypeByGroupId(cropGroupId);
    }

    public async Task<decimal> FetchCropTypeDefaultYieldByCropTypeId(int cropTypeId)
    {
        _logger.LogTrace("Fetching default yield for CropTypeId: {CropTypeId}", cropTypeId);
        return await _cropService.FetchCropTypeDefaultYieldByCropTypeId(cropTypeId);
    }

    public async Task<(List<CropTypeLinkingResponse>, Error)> FetchCropTypeLinking()
    {
        _logger.LogTrace("Fetching crop type linking");
        return await _cropService.FetchCropTypeLinking();
    }

    public async Task<(DefoliationSequenceResponse, Error)> FetchDefoliationSequencesById(int defoliationId)
    {
        _logger.LogTrace("Fetching defoliation sequence by ID: {DefoliationId}", defoliationId);
        return await _cropService.FetchDefoliationSequencesById(defoliationId);
    }

    public async Task<(List<DefoliationSequenceResponse>, Error)> FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(int swardTypeId, int swardManagementId, int numberOfCut, bool isNewSward)
    {
        _logger.LogTrace("Fetching defoliation sequences for SwardTypeId: {SwardTypeId}, SwardManagementId: {SwardManagementId}, NumberOfCut: {NumberOfCut}, IsNewSward: {IsNewSward}", swardTypeId, swardManagementId, numberOfCut, isNewSward);
        return await _cropService.FetchDefoliationSequencesBySwardManagementIdAndNumberOfCut(swardTypeId, swardManagementId, numberOfCut, isNewSward);
    }

    public async Task<(List<GrassGrowthClassResponse>, Error)> FetchGrassGrowthClass(List<int> fieldIds)
    {
        _logger.LogTrace("Fetching grass growth class for FieldIds: {FieldIds}", string.Join(", ", fieldIds));
        return await _cropService.FetchGrassGrowthClass(fieldIds);
    }

    public async Task<List<GrassSeasonResponse>> FetchGrassSeasons()
    {
        _logger.LogTrace("Fetching grass seasons");
        return await _cropService.FetchGrassSeasons();
    }

    public async Task<(List<HarvestYearPlanResponse>, Error)> FetchHarvestYearPlansByFarmId(int harvestYear, int farmId)
    {
        _logger.LogTrace("Fetching harvest year plans for FarmId: {FarmId}, HarvestYear: {HarvestYear}", farmId, harvestYear);
        return await _cropService.FetchHarvestYearPlansByFarmId(harvestYear, farmId);
    }

    public async Task<(HarvestYearResponseHeader, Error)> FetchHarvestYearPlansDetailsByFarmId(int harvestYear, int farmId)
    {
        _logger.LogTrace("Fetching harvest year plan details for FarmId: {FarmId}, HarvestYear: {HarvestYear}", farmId, harvestYear);
        return await _cropService.FetchHarvestYearPlansDetailsByFarmId(harvestYear, farmId);
    }

    public async Task<(List<ManagementPeriod>, Error)> FetchManagementperiodByCropId(int cropId, bool isShortSummary)
    {
        _logger.LogTrace("Fetching management periods for CropId: {CropId}, IsShortSummary: {IsShortSummary}", cropId, isShortSummary);
        return await _cropService.FetchManagementperiodByCropId(cropId, isShortSummary);
    }

    public async Task<(ManagementPeriod, Error)> FetchManagementperiodById(int id)
    {
        _logger.LogTrace("Fetching management period by ID: {Id}", id);
        return await _cropService.FetchManagementperiodById(id);
    }

    public async Task<List<PlanSummaryResponse>> FetchPlanSummaryByFarmId(int farmId, int type)
    {
        _logger.LogTrace("Fetching plan summary for FarmId: {FarmId}, Type: {Type}", farmId, type);
        return await _cropService.FetchPlanSummaryByFarmId(farmId, type);
    }

    public async Task<List<PotatoVarietyResponse>> FetchPotatoVarieties()
    {
        _logger.LogTrace("Fetching potato varieties");
        return await _cropService.FetchPotatoVarieties();
    }

    public async Task<(List<PotentialCutResponse>, Error)> FetchPotentialCutsBySwardTypeIdAndSwardManagementId(int swardTypeId, int swardManagementId)
    {
        _logger.LogTrace("Fetching potential cuts for SwardTypeId: {SwardTypeId}, SwardManagementId: {SwardManagementId}", swardTypeId, swardManagementId);
        return await _cropService.FetchPotentialCutsBySwardTypeIdAndSwardManagementId(swardTypeId, swardManagementId);
    }

    public async Task<(List<RecommendationHeader>, Error)> FetchRecommendationByFieldIdAndYear(int fieldId, int harvestYear)
    {
        _logger.LogTrace("Fetching recommendations for FieldId: {FieldId}, HarvestYear: {HarvestYear}", fieldId, harvestYear);
        return await _cropService.FetchRecommendationByFieldIdAndYear(fieldId, harvestYear);
    }

    public async Task<List<int>> FetchSecondCropListByFirstCropId(int firstCropTypeId)
    {
        _logger.LogTrace("Fetching second crop list for FirstCropTypeId: {FirstCropTypeId}", firstCropTypeId);
        return await _cropService.FetchSecondCropListByFirstCropId(firstCropTypeId);
    }

    public async Task<(SwardManagementResponse, Error)> FetchSwardManagementBySwardManagementId(int swardManagementId)
    {
        _logger.LogTrace("Fetching sward management by ID: {SwardManagementId}", swardManagementId);
        return await _cropService.FetchSwardManagementBySwardManagementId(swardManagementId);
    }

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagementBySwardTypeId(int swardTypeId)
    {
        _logger.LogTrace("Fetching sward managements for SwardTypeId: {SwardTypeId}", swardTypeId);
        return await _cropService.FetchSwardManagementBySwardTypeId(swardTypeId);
    }

    public async Task<(List<SwardManagementResponse>, Error)> FetchSwardManagements()
    {
        _logger.LogTrace("Fetching sward managements");
        return await _cropService.FetchSwardManagements();
    }

    public async Task<(SwardTypeResponse, Error)> FetchSwardTypeBySwardTypeId(int swardTypeId)
    {
        _logger.LogTrace("Fetching sward type by ID: {SwardTypeId}", swardTypeId);
        return await _cropService.FetchSwardTypeBySwardTypeId(swardTypeId);
    }

    public async Task<(List<SwardTypeResponse>, Error)> FetchSwardTypes()
    {
        _logger.LogTrace("Fetching sward types");
        return await _cropService.FetchSwardTypes();
    }

    public async Task<(List<YieldRangesEnglandAndWalesResponse>, Error)> FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(int sequenceId, int grassGrowthClassId)
    {
        _logger.LogTrace("Fetching yield ranges for SequenceId: {SequenceId}, GrassGrowthClassId: {GrassGrowthClassId}", sequenceId, grassGrowthClassId);
        return await _cropService.FetchYieldRangesEnglandAndWalesBySequenceIdAndGrassGrowthClassId(sequenceId, grassGrowthClassId);
    }

    public async Task<(bool, Error)> IsCropsGroupNameExistForUpdate(string cropIds, string cropGroupName, int year, int farmId)
    {
        _logger.LogTrace("Checking if crop group name exists for update: {CropGroupName} in FarmId: {FarmId}, Year: {Year}", cropGroupName, farmId, year);
        return await _cropService.IsCropsGroupNameExistForUpdate(cropIds, cropGroupName, year, farmId);
    }

    public async Task<(bool, Error)> MergeCrop(string cropData)
    {
        _logger.LogTrace("Merging crop data");
        return await _cropService.MergeCrop(cropData);
    }

    public async Task<(string, Error)> RemoveCropPlan(List<int> cropIds)
    {
        _logger.LogTrace("Removing crop plans for CropIds: {CropIds}", string.Join(", ", cropIds));
        return await _cropService.RemoveCropPlan(cropIds);
    }

    public async Task<(List<Crop>, Error)> UpdateCrop(string cropData)
    {
        _logger.LogTrace("Updating crop data");
        return await _cropService.UpdateCrop(cropData);
    }

    public async Task<SnsAnalysis> FetchSnsAnalysisByCropIdAsync(int cropId)
    {
        _logger.LogTrace("SnsAnalysisLogic : FetchSnsAnalysisByCropIdAsync() called");
        return await _snsAnalysisService.FetchSnsAnalysisByCropIdAsync(cropId);
    }
}
