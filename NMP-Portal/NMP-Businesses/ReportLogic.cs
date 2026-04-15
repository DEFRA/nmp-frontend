using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NMP.Application;
using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Attributes;
using NMP.Core.Interfaces;

namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class ReportLogic(ILogger<ReportLogic> logger, IReportService reportService, ICropLogic cropLogic, IFieldLogic fieldLogic, IFertiliserManureLogic fertiliserManureLogic, IFarmLogic farmLogic, IOrganicManureLogic organicManureLogic) : IReportLogic
{
    private readonly ILogger<ReportLogic> _logger = logger;
    private readonly IReportService _reportService = reportService;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    private readonly IFarmLogic _farmLogic = farmLogic;
    private readonly IFertiliserManureLogic _fertiliserManureLogic = fertiliserManureLogic;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;

    public async Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        _logger.LogTrace("Adding N Loading Farm details");
        return await _reportService.AddNutrientsLoadingFarmDetailsAsync(nutrientsLoadingFarmDetailsData);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        _logger.LogTrace("Adding N Loading live stock");
        return await _reportService.AddNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStockData);
    }

    public async Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        _logger.LogTrace("Adding N Loading manures");
        return await _reportService.AddNutrientsLoadingManuresAsync(nutrientsLoadingManure);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId)
    {
        _logger.LogTrace("Deleting N Loading live stock by Id");
        return await _reportService.DeleteNutrientsLoadingLivestockByIdAsync(nutrientsLoadingLivestockId);
    }

    public async Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId)
    {
        _logger.LogTrace("Deleting N loading manure by Id");
        return await _reportService.DeleteNutrientsLoadingManureByIdAsync(nutrientsLoadingManureId);
    }

    public async Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYear(int farmId, int year)
    {
        _logger.LogTrace("Fetching live stock by Farm Id and Year");
        return await _reportService.FetchLivestockByFarmIdAndYear(farmId, year);
    }

    public async Task<(CommonResponse, Error)> FetchLivestockGroupById(int livestockGroupId)
    {
        _logger.LogTrace("Fetching livestock group by Id");
        return await _reportService.FetchLivestockGroupById(livestockGroupId);
    }

    public async Task<(List<CommonResponse>, Error)> FetchLivestockGroupList()
    {
        _logger.LogTrace("Fetch livestock group list");
        return await _reportService.FetchLivestockGroupList();
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypes()
    {
        _logger.LogTrace("Fetching livestock types");
        return await _reportService.FetchLivestockTypes();
    }

    public async Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupId(int livestockGroupId)
    {
        _logger.LogTrace("Fetching livestock types by group Id");
        return await _reportService.FetchLivestockTypesByGroupId(livestockGroupId);
    }

    public async Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching N Loading farm details by FarmId");
        return await _reportService.FetchNutrientsLoadingFarmDetailsByFarmId(farmId);
    }

    public async Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year)
    {
        _logger.LogTrace("Fetching N Loading farm details by FarmId and Year");
        return await _reportService.FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(farmId, year);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id)
    {
        _logger.LogTrace("Fetching N Loading livestock by Id");
        return await _reportService.FetchNutrientsLoadingLiveStockByIdAsync(id);
    }

    public async Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmId(int farmId)
    {
        _logger.LogTrace("Fetching N Loading Manures by FarmId");
        return await _reportService.FetchNutrientsLoadingManuresByFarmId(farmId);
    }

    public async Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id)
    {
        _logger.LogTrace("Fetching N Loading Manures by Id");
        return await _reportService.FetchNutrientsLoadingManuresByIdAsync(id);
    }

    public async Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData)
    {
        _logger.LogTrace("Update N Loading Farm Details");
        return await _reportService.UpdateNutrientsLoadingFarmDetailsAsync(nutrientsLoadingFarmDetailsData);
    }

    public async Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData)
    {
        _logger.LogTrace("Update N Loading livestack");
        return await _reportService.UpdateNutrientsLoadingLiveStockAsync(nutrientsLoadingLiveStockData);
    }

    public async Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure)
    {
        _logger.LogTrace("Update Nutrients Loading Manures");
        return await _reportService.UpdateNutrientsLoadingManuresAsync(nutrientsLoadingManure);
    }

    public static (int soilTypeAdjustment, int millingWheat, decimal yieldAdjustment)
   BindAdjustmentsForCerealCropEngAndWales(Crop crop, Field field)
    {
        int soil = GetSoilAdjustment(crop, field);
        int milling = GetMillingAdjustment(crop);
        decimal yield = GetYieldAdjustment(crop);

        return (soil, milling, yield);
    }
    private static int GetSoilAdjustment(Crop crop, Field field)
    {
        bool isShallow =
            field.SoilTypeID == (int)NMP.Commons.Enums.SoilTypeEngland.Shallow;

        return (crop.CropTypeID.Value is
                (int)NMP.Commons.Enums.CropTypes.WinterWheat or
                (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat or
                (int)NMP.Commons.Enums.CropTypes.WinterBarley or
                (int)NMP.Commons.Enums.CropTypes.WholecropWinterBarley)
                && isShallow
            ? 20
            : 0;
    }
    private static int GetMillingAdjustment(Crop crop)
    {
        bool isMilling = crop.CropInfo1 ==
            (int)NMP.Commons.Enums.CropInfoOne.Milling;

        return (crop.CropTypeID.Value is
                (int)NMP.Commons.Enums.CropTypes.WinterWheat or
                (int)NMP.Commons.Enums.CropTypes.SpringWheat)
                && isMilling
            ? 40
            : 0;
    }

    private static decimal GetYieldAdjustment(Crop crop)
    {
        decimal yield = crop.Yield ?? 0;

        return crop.CropTypeID.Value switch
        {
            (int)NMP.Commons.Enums.CropTypes.WinterWheat =>
                CalculateYield(yield, 8.0m, 2),

            (int)NMP.Commons.Enums.CropTypes.SpringWheat =>
                CalculateYield(yield, 7.0m, 2),

            (int)NMP.Commons.Enums.CropTypes.WinterBarley =>
                CalculateYield(yield, 6.5m, 2),

            (int)NMP.Commons.Enums.CropTypes.SpringBarley =>
                CalculateYield(yield, 5.5m, 2),

            (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape =>
                CalculateYield(yield, 3.5m, 6),

            _ => 0
        };
    }
    private static decimal CalculateYield(decimal yield, decimal baseValue, int multiplier)
    {
        return yield > baseValue
            ? (int)Math.Round(((yield - baseValue) / 0.1m) * multiplier)
            : 0;
    }
    public (int yieldAdjustment, int marketAdjustment, int rainfallAdjustment)
BindAdjustmentsForScotland(
    Crop crop,
    int? winterRainfall,
    int? nResidueGroup,
    int soilTypeId,
    decimal? standardYield)
    {
        int market = GetScotlandMarketAdjustment(crop);
        int rainfall = GetRainfallAdjustment(winterRainfall, nResidueGroup, soilTypeId);
        int yield = GetScotlandYieldAdjustment(crop, standardYield);

        return (yield, market, rainfall);
    }
    private static int GetScotlandMarketAdjustment(Crop crop)
    {
        bool isMilling = crop.CropInfo1 ==
            (int)NMP.Commons.Enums.CropInfoOne.Milling;

        bool isHighNGrain = crop.CropInfo1 ==
            (int)NMP.Commons.Enums.CropInfoOne.HighNGrainDistilling;

        int cropType = crop.CropTypeID.Value;

        if (isMilling && IsMillingCrop(cropType))
            return 40;

        if (isHighNGrain && IsDistillingCrop(cropType))
            return 15;

        return 0;
    }
    private static bool IsMillingCrop(int cropType) =>
    cropType is
        (int)NMP.Commons.Enums.CropTypes.WinterWheat or
        (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat or
        (int)NMP.Commons.Enums.CropTypes.SpringWheat or
        (int)NMP.Commons.Enums.CropTypes.WholecropSpringWheat or
        (int)NMP.Commons.Enums.CropTypes.WheatSpringUndersown;

    private static bool IsDistillingCrop(int cropType) =>
        cropType is
            (int)NMP.Commons.Enums.CropTypes.SpringBarley or
            (int)NMP.Commons.Enums.CropTypes.WholecropSpringBarley or
            (int)NMP.Commons.Enums.CropTypes.BarleySpringUndersown;
    private static int GetRainfallAdjustment(
    int? winterRainfall,
    int? nResidueGroup,
    int soilTypeId)
    {
        if (winterRainfall != 500 || nResidueGroup == 1)
            return 0;

        bool isLightSoil = soilTypeId is
            (int)NMP.Commons.Enums.SoilTypeScotland.SandyLoam or
            (int)NMP.Commons.Enums.SoilTypeScotland.Sand or
            (int)NMP.Commons.Enums.SoilTypeScotland.Shallow;

        bool isHeavySoil = soilTypeId is
            (int)NMP.Commons.Enums.SoilTypeScotland.Peaty or
            (int)NMP.Commons.Enums.SoilTypeScotland.Humose or
            (int)NMP.Commons.Enums.SoilTypeScotland.OtherMineral;

        if (isLightSoil)
        {
            return nResidueGroup switch
            {
                2 => 10,
                3 or 4 or 5 or 6 => 20,
                _ => 0
            };
        }

        if (isHeavySoil)
            return 10;

        return 0;
    }
    private static int GetScotlandYieldAdjustment(Crop crop, decimal? standardYield)
    {
        if (crop.Yield == null || standardYield == null)
            return 0;

        if (crop.Yield <= standardYield)
            return 0;

        decimal yield = crop.Yield.Value;
        decimal baseVal = standardYield.Value;

        int cropType = crop.CropTypeID.Value;

        return cropType switch
        {
            (int)NMP.Commons.Enums.CropTypes.WinterWheat => CalculateScotlandYieldAdjustment(yield, baseVal, 2),
            (int)NMP.Commons.Enums.CropTypes.SpringWheat => CalculateScotlandYieldAdjustment(yield, baseVal, 2),
            (int)NMP.Commons.Enums.CropTypes.WheatSpringUndersown => CalculateScotlandYieldAdjustment(yield, baseVal, 2),

            (int)NMP.Commons.Enums.CropTypes.WinterBarley => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),
            (int)NMP.Commons.Enums.CropTypes.SpringBarley => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),
            (int)NMP.Commons.Enums.CropTypes.BarleySpringUndersown => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),

            (int)NMP.Commons.Enums.CropTypes.WinterOats => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),
            (int)NMP.Commons.Enums.CropTypes.SpringOats => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),
            (int)NMP.Commons.Enums.CropTypes.OatsSpringUndersown => CalculateScotlandYieldAdjustment(yield, baseVal, 1.5m),

            (int)NMP.Commons.Enums.CropTypes.SpringOilseedRape => CalculateScotlandYieldAdjustment(yield, baseVal, 30),

            _ => 0
        };
    }
    private static int CalculateScotlandYieldAdjustment(decimal yield, decimal baseVal, decimal multiplier)
    {
        return (int)Math.Round(((yield - baseVal) / 0.1m) * multiplier);
    }
    // ===============================
    // SCOTLAND NMAX LOGIC (REFRACTORED)
    // ===============================

    public async Task<(List<FieldDetails>, decimal, int, int)>
    BindNmaxResponseForScotland(
        ReportViewModel model,
        Crop crop,
        Field field,
        List<FieldDetails> fieldDetail,
        decimal? defaultYield, string previousCrop, List<ScotlandNMaxValue>? scotlandNMaxValue)
    {
        decimal yieldAdjustment = 0;
        int marketAdjustment = 0;
        int rainfallAdjustment = 0;

        var soilType = await GetSoilType(field);

        var excessRain = await GetExcessRainfall(model);

        var (recommendation, mpId) = await GetRecommendation(crop);

        if (recommendation == null || mpId == null)
            return (fieldDetail, yieldAdjustment, marketAdjustment, rainfallAdjustment);

        if (ShouldCalculateScotlandAdjustment(scotlandNMaxValue, crop))
        {
            (yieldAdjustment, marketAdjustment, rainfallAdjustment) =
                CalculateScotlandAdjustments(
                    crop,
                    field,
                    excessRain,
                    recommendation,
                    defaultYield);
        }

        AddFieldDetails(
            fieldDetail,
            field,
            soilType,
            previousCrop,
            recommendation);

        return (fieldDetail, yieldAdjustment, marketAdjustment, rainfallAdjustment);
    }


    // ===============================
    // HELPERS
    // ===============================

    private async Task<SoilTypesResponse?> GetSoilType(Field field)
    {
        var soilTypes = await _fieldLogic.FetchSoilTypes();
        return soilTypes.FirstOrDefault(x => x.SoilTypeId == field.SoilTypeID);
    }


    private async Task<ExcessRainfalls?> GetExcessRainfall(ReportViewModel model)
    {
        var (rain, _) = await _farmLogic.FetchExcessRainfallsAsync(
            model.FarmId.Value,
            model.Year.Value);

        return rain;
    }

    private async Task<(Recommendation?, int?)> GetRecommendation(Crop crop)
    {
        var (list, _) = await _cropLogic.FetchManagementperiodByCropId(crop.ID.Value, true);

        if (!list.Any())
            return (null, null);

        var mpId = list[0].ID;

        if (mpId == null)
            return (null, null);

        (Recommendation? rec, _) =  await _cropLogic.FetchRecommendationByManagementPeriodId(mpId.Value);

        return (rec, mpId);
    }

    private static bool ShouldCalculateScotlandAdjustment(
        List<ScotlandNMaxValue>? scotlandValues,
        Crop crop)
    {
        return scotlandValues?.Any(x => x.CropTypeID == crop.CropTypeID) == true;
    }

    private (decimal, int, int) CalculateScotlandAdjustments(
        Crop crop,
        Field field,
        ExcessRainfalls? rain,
        Recommendation recommendation,
        decimal? defaultYield)
    {
        return BindAdjustmentsForScotland(
            crop,
            rain?.WinterRainfall,
            recommendation.NIndex != null
                ? Convert.ToInt32(recommendation.NIndex)
                : null,
            field.SoilTypeID.Value,
            defaultYield);
    }

    private void AddFieldDetails(
        List<FieldDetails> fieldDetail,
        Field field,
        SoilTypesResponse? soilType,
        string previousCrop,
        Recommendation recommendation)
    {
        fieldDetail.Add(new FieldDetails
        {
            FieldName = field.Name ?? "",
            CroppedArea = field.CroppedArea,
            SoilType = soilType?.SoilType,
            PreviousCrop = previousCrop,
            NitrogenResidueGroup = recommendation.NIndex != null
                ? Convert.ToInt32(recommendation.NIndex)
                : null
        });
    }


    public async Task<(decimal?, decimal?)> FetchTotalNitroegen(List<ManagementPeriod> ManPeriodList)
    {
        decimal? totalFertiliserN = null;
        decimal? totalOrganicAvailableN = null;
        foreach (var managementPeriod in ManPeriodList)
        {
            (decimal? totalNitrogen, _) = await _fertiliserManureLogic.FetchTotalNByManagementPeriodID(managementPeriod.ID.Value);
            if (totalNitrogen != null)
            {
                if (totalFertiliserN == null)
                {
                    totalFertiliserN = 0;
                }
                totalFertiliserN = totalFertiliserN + totalNitrogen;
            }
        }
        foreach (var managementPeriod in ManPeriodList)
        {
            (decimal? totalNitrogen, _) = await _organicManureLogic.FetchAvailableNByManagementPeriodID(managementPeriod.ID.Value);
            if (totalNitrogen != null)
            {
                if (totalOrganicAvailableN == null)
                {
                    totalOrganicAvailableN = 0;
                }
                totalOrganicAvailableN = totalOrganicAvailableN + totalNitrogen;
            }
        }
        return (totalFertiliserN, totalOrganicAvailableN);
    }

    // ===============================
    // ENGLAND & WALES ADJUSTMENTS
    // ===============================

    public async Task<(
        int soilTypeAdjustment,
        int millingWheat,
        decimal yieldAdjustment,
        int paperCrumbleOrStrawMulch,
        decimal grassCut)>
    BindAdjustmentsForEnglandAndWales(Crop crop, Field field, int year)
    {
        bool manureTypeCondition =
            await CheckManureTypeCondition(field.ID.Value, year);

        int soilTypeAdjustment = 0;
        int millingWheat = 0;
        decimal yieldAdjustment = 0;
        int paperCrumbleOrStrawMulch = 0;
        decimal grassCut = 0;

        int cropType = crop.CropTypeID.Value;

        // =========================
        // VEGETABLE / ROOT CROPS
        // =========================
        if (IsVegetableOrRootCrop(cropType))
        {
            if (manureTypeCondition)
                paperCrumbleOrStrawMulch = 80;
        }

        // =========================
        // GRASS
        // =========================
        else if (cropType == (int)NMP.Commons.Enums.CropTypes.Grass)
        {
            if (manureTypeCondition)
                paperCrumbleOrStrawMulch = 80;

            if (HasThreeOrMoreCuts(crop))
                grassCut = 40;
        }

        // =========================
        // CEREAL CROPS
        // =========================
        else if (IsCerealCrop(cropType))
        {
            if (manureTypeCondition)
                paperCrumbleOrStrawMulch = 80;

            (soilTypeAdjustment, millingWheat, yieldAdjustment) =
                BindAdjustmentsForCerealCropEngAndWales(crop, field);
        }

        return (
            soilTypeAdjustment,
            millingWheat,
            yieldAdjustment,
            paperCrumbleOrStrawMulch,
            grassCut
        );
    }


    // ===============================
    // HELPERS
    // ===============================

    private static bool IsVegetableOrRootCrop(int cropType) =>
        cropType is
            (int)NMP.Commons.Enums.CropTypes.SugarBeet or
            (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup1 or
            (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup2 or
            (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup3 or
            (int)NMP.Commons.Enums.CropTypes.PotatoVarietyGroup4 or
            (int)NMP.Commons.Enums.CropTypes.ForageMaize or
            (int)NMP.Commons.Enums.CropTypes.WinterBeans or
            (int)NMP.Commons.Enums.CropTypes.SpringBeans or
            (int)NMP.Commons.Enums.CropTypes.Peas or
            (int)NMP.Commons.Enums.CropTypes.Asparagus or
            (int)NMP.Commons.Enums.CropTypes.Carrots or
            (int)NMP.Commons.Enums.CropTypes.Radish or
            (int)NMP.Commons.Enums.CropTypes.Swedes or
            (int)NMP.Commons.Enums.CropTypes.CelerySelfBlanching or
            (int)NMP.Commons.Enums.CropTypes.Courgettes or
            (int)NMP.Commons.Enums.CropTypes.DwarfBeans or
            (int)NMP.Commons.Enums.CropTypes.Lettuce or
            (int)NMP.Commons.Enums.CropTypes.BulbOnions or
            (int)NMP.Commons.Enums.CropTypes.SaladOnions or
            (int)NMP.Commons.Enums.CropTypes.Parsnips or
            (int)NMP.Commons.Enums.CropTypes.RunnerBeans or
            (int)NMP.Commons.Enums.CropTypes.Sweetcorn or
            (int)NMP.Commons.Enums.CropTypes.Turnips or
            (int)NMP.Commons.Enums.CropTypes.Beetroot or
            (int)NMP.Commons.Enums.CropTypes.BrusselSprouts or
            (int)NMP.Commons.Enums.CropTypes.Cabbage or
            (int)NMP.Commons.Enums.CropTypes.Calabrese or
            (int)NMP.Commons.Enums.CropTypes.Cauliflower or
            (int)NMP.Commons.Enums.CropTypes.Leeks;


    private static bool IsCerealCrop(int cropType) =>
        cropType is
            (int)NMP.Commons.Enums.CropTypes.WinterWheat or
            (int)NMP.Commons.Enums.CropTypes.SpringWheat or
            (int)NMP.Commons.Enums.CropTypes.WinterBarley or
            (int)NMP.Commons.Enums.CropTypes.SpringBarley or
            (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape or
            (int)NMP.Commons.Enums.CropTypes.WholecropSpringBarley or
            (int)NMP.Commons.Enums.CropTypes.WholecropSpringWheat or
            (int)NMP.Commons.Enums.CropTypes.WholecropWinterBarley or
            (int)NMP.Commons.Enums.CropTypes.WholecropWinterWheat;


    private static bool HasThreeOrMoreCuts(Crop crop)
    {
        int[] ids = { 10, 11, 32, 33, 56, 57, 78, 79 };

        return crop.DefoliationSequenceID.HasValue &&
               ids.Contains(crop.DefoliationSequenceID.Value);
    }
    private async Task<bool> CheckManureTypeCondition(int fieldId, int year)
    {
        (List<int> currentYearManureTypeIds, _) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), year, false);
        (List<int> previousYearManureTypeIds, _) = await _organicManureLogic.FetchManureTypsIdsByFieldIdYearAndConfirmFromOrgManure(Convert.ToInt32(fieldId), year - 1, false);
        bool manureTypeCondition = false;
        if (currentYearManureTypeIds.Count > 0)
        {
            foreach (var Ids in currentYearManureTypeIds)
            {
                if (Ids == (int)NMP.Commons.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                    Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                {
                    manureTypeCondition = true;
                }
            }
        }
        if (previousYearManureTypeIds.Count > 0)
        {
            foreach (var Ids in previousYearManureTypeIds)
            {
                if (Ids == (int)NMP.Commons.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                    Ids == (int)NMP.Commons.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                {
                    manureTypeCondition = true;
                }
            }
        }
        return manureTypeCondition;
    }
    public async Task ProcessEnglandAndWales(
       Crop crop,
       Field field,
       ReportViewModel model,
       HarvestYearPlanResponse cropData,
        string cropTypeName,
        int nMaxLimitForCropType,
       List<NMaxLimitReportResponse> nMaxList)
    {
        cropTypeName = cropData.CropTypeName;

        (int soil, int milling, decimal yield, int paper, decimal grass) =
                   await BindAdjustmentsForEnglandAndWales(
                       crop,
                       field,
                       model.Year.Value);

        nMaxLimitForCropType = (int)Math.Round(
            nMaxLimitForCropType + soil + yield + milling + paper + grass,
            0);

        var data = BindNMaxLimitReportResponse(field, crop, nMaxLimitForCropType, yield);

        data.CropTypeName = cropTypeName;
        data.SoilTypeAdjustment = soil;
        data.MillingWheat = milling;
        data.AdjustmentForThreeOrMoreCuts = grass;
        data.PaperCrumbleOrStrawMulch = paper;
        data.AdjustedNMaxLimit = nMaxLimitForCropType;

        nMaxList.Add(data);
    }
    private NMaxLimitReportResponse BindNMaxLimitReportResponse(Field field, Crop crop, int nMaxLimitForCropType, decimal yieldAdjustment)
    {
        return new NMaxLimitReportResponse
        {
            FieldId = field.ID.Value,
            FieldName = field.Name ?? string.Empty,
            CropArea = field.CroppedArea.HasValue ? field.CroppedArea.Value : default(decimal),
            CropYield = crop.Yield != null ? crop.Yield.Value : null,
            YieldAdjustment = yieldAdjustment,
            MaximumLimitForNApplied = field.CroppedArea.HasValue ? (int)Math.Round(nMaxLimitForCropType * field.CroppedArea.Value, 0) : 0
        };

    }
    // ======================================
    // SCOTLAND
    // ======================================
    public async Task ProcessScotland(
        ReportViewModel model,
        HarvestYearPlanResponse cropData,
        List<FieldDetails> fieldDetail,
         int nMaxLimitForCropType,
        List<NMaxLimitReportResponse> nMaxList,
        string previousCrop,
        List<ScotlandNMaxValue>? scotlandNMaxValue)
    {
        (Crop? crop, _) = await _cropLogic.FetchCropById(cropData.CropID);
        Field field = await _fieldLogic.FetchFieldByFieldId(cropData.FieldID);
        decimal defaultYield =
            await _cropLogic.FetchCropTypeDefaultYieldByCropTypeId(
                crop.CropTypeID.Value,
                true);

        (List<FieldDetails> _, decimal yield, int market, int rainfall) =
            await BindNmaxResponseForScotland(
                model,
                crop,
                field,
                fieldDetail,
                defaultYield,
                previousCrop,
                scotlandNMaxValue);
                

        int nmaxLimit = nMaxLimitForCropType;
        nMaxLimitForCropType = (int)Math.Round(
            nMaxLimitForCropType + market + yield + rainfall,
            0);

        var data = BindNMaxLimitReportResponse(
            field,
            crop,
            nMaxLimitForCropType,
            yield);

        data.CropTypeName = cropData.CropTypeName;
        data.MarketAdjustment = market;
        data.WinterRainfallAdjustment = rainfall;
        data.StandardRate = nmaxLimit;
        data.AdjustedNMaxLimit = (int)Math.Round(market + rainfall + yield, 0);

        nMaxList.Add(data);
    }
    public async Task<string> GetPreviousCropAsync(int fieldId, int year)
    {
        // Step 1: Try crop plan
        var (cropList, _) = await _cropLogic
            .FetchCropPlanByFieldIdAndYear(fieldId, year - 1);

        if (cropList?.Any() == true)
        {
            var cropTypeId = cropList[0].CropTypeID;
            if (cropTypeId != null)
                return await _fieldLogic.FetchCropTypeById(cropTypeId.Value);
        }

        // Step 2: Fallback to previous cropping data
        var (previousCroppingList, _) = await _cropLogic
            .FetchDataByFieldId(fieldId, year - 1);

        if (previousCroppingList?.Any() == true)
        {
            var cropTypeId = previousCroppingList[0].CropTypeID;
            if (cropTypeId != null)
                return await _fieldLogic.FetchCropTypeById(cropTypeId.Value);
        }

        return string.Empty;
    }
}

