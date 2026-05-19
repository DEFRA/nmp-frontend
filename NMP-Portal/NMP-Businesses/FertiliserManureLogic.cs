using Microsoft.AspNetCore.Mvc;
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
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
namespace NMP.Businesses;

[Business(ServiceLifetime.Transient)]
public class FertiliserManureLogic(ILogger<FertiliserManureLogic> logger, IFertiliserManureService fertiliserManureService, IWarningLogic warningLogic, IOrganicManureLogic organicManureLogic, ICropLogic cropLogic, IFieldLogic fieldLogic) : IFertiliserManureLogic
{
    private readonly ILogger<FertiliserManureLogic> _logger = logger;
    private readonly IFertiliserManureService _fertiliserManureService = fertiliserManureService;
    private readonly IWarningLogic _warningLogic = warningLogic;
    private readonly IOrganicManureLogic _organicManureLogic = organicManureLogic;
    private readonly ICropLogic _cropLogic = cropLogic;
    private readonly IFieldLogic _fieldLogic = fieldLogic;
    public async Task<(List<FertiliserManure>, Error)> AddFertiliserManureAsync(string fertiliserManure)
    {
        _logger.LogTrace("Adding Fertiliser Manure");
        return await _fertiliserManureService.AddFertiliserManureServiceAsync(fertiliserManure);
    }

    public async Task<(string, Error)> DeleteFertiliserByIdAsync(string fertiliserIds)
    {
        _logger.LogTrace("Deleting Fertiliser by Id");
        return await _fertiliserManureService.DeleteFertiliserByIdServiceAsync(fertiliserIds);
    }

    public async Task<(List<ManureCropTypeResponse>, Error)> FetchCropTypeByFarmIdAndHarvestYear(int farmId, int harvestYear)
    {
        _logger.LogTrace("Fetching Crop Type by FarmId:{FarmId} and HarvestYear:{HarvestYear}", farmId, harvestYear);
        return await _fertiliserManureService.FetchCropTypeByFarmIdAndHarvestYearServiceAsync(farmId, harvestYear);
    }

    public async Task<(FertiliserManureDataViewModel, Error)> FetchFertiliserByIdAsync(int fertiliserId)
    {
        _logger.LogTrace("Fetching Fertiliser by Id");
        return await _fertiliserManureService.FetchFertiliserByIdServiceAsync(fertiliserId);
    }

    public async Task<(List<CommonResponse>, Error)> FetchFieldByFarmIdAndHarvestYearAndCropGroupName(int harvestYear, int farmId, string? cropGroupName)
    {
        _logger.LogTrace("Fetching Field by Farm Id, Harvest Year and Crop Group name");
        return await _fertiliserManureService.FetchFieldByFarmIdAndHarvestYearAndCropGroupNameServiceAsync(harvestYear, farmId, cropGroupName);
    }

    public async Task<(List<FertiliserAndOrganicManureUpdateResponse>, Error)> FetchFieldWithSameDateAndNutrient(int fertiliserId, int farmId, int harvestYear)
    {
        _logger.LogTrace("Fetching field with samedate and nutrient");
        return await _fertiliserManureService.FetchFieldWithSameDateAndNutrientServiceAsync(fertiliserId, farmId, harvestYear);
    }

    public async Task<(List<InOrganicManureDurationResponse>, Error)> FetchInOrganicManureDurations()
    {
        _logger.LogTrace("Fetching inorganic manure durations");
        return await _fertiliserManureService.FetchInOrganicManureDurationsServiceAsync();
    }

    public async Task<(InOrganicManureDurationResponse, Error)> FetchInOrganicManureDurationsById(int id)
    {
        _logger.LogTrace("Fetching Inorganic manure duration by Id");
        return await _fertiliserManureService.FetchInOrganicManureDurationsByIdServiceAsync(id);
    }

    public async Task<(List<int>, Error)> FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupName(int harvestYear, string fieldIds, string? cropGroupName, int? cropOrder)
    {
        _logger.LogTrace("Fetching ManagementId by Field Id and Harvest year and Crop group name");
        return await _fertiliserManureService.FetchManagementIdsByFieldIdAndHarvestYearAndCropGroupNameServiceAsync(harvestYear, fieldIds, cropGroupName, cropOrder);
    }

    public async Task<(decimal, Error)> FetchTotalNBasedOnFieldIdAndAppDate(int fieldId, DateTime startDate, DateTime endDate, int? fertiliserId, bool confirm)
    {
        _logger.LogTrace("Fetch total N based on Field Id and application date");
        return await _fertiliserManureService.FetchTotalNBasedOnFieldIdAndAppDateServiceAsync(fieldId, startDate, endDate, fertiliserId, confirm);
    }

    public async Task<(decimal?, Error)> FetchTotalNByManagementPeriodID(int managementPeriodID)
    {
        _logger.LogTrace("Fetching total N by management perios Id");
        return await _fertiliserManureService.FetchTotalNByManagementPeriodIDServiceAsync(managementPeriodID);
    }

    public async Task<(List<FertiliserManure>, Error?)> UpdateFertiliser(string fertliserData)
    {
        _logger.LogTrace("Updating fertiliser");
        return await _fertiliserManureService.UpdateFertiliserServiceAsync(fertliserData);
    }
    public async Task<(string?, Error?)> FetchFertiliserManureClosedPeriod(int countryId, int cropTypeId, int? nvzProgramId)
    {
        _logger.LogTrace("Fetch fertiliser manure closed period");
        return await _fertiliserManureService.FetchFertiliserManureClosedPeriodServiceAsync(countryId, cropTypeId, nvzProgramId);
    }

    public async Task<(decimal?, Error?)> FetchTotalNByManagementPeriodIDIsAutumn(int managementPeriodID, bool isAutumn)
    {
        _logger.LogTrace("Fetching total N by management perios Id and isAutumn");
        return await _fertiliserManureService.FetchTotalNByManagementPeriodIDIsAutumnServiceAsync(managementPeriodID, isAutumn);
    }

    //warning logic
    public (string, string, string) BindStartEndDateAndWarningPeriod(FertiliserManureViewModel model, DateTime endDate, string closedPeriod)
    {
        string startPeriod = string.Empty; string endPeriod = string.Empty;
        string warningPeriod = string.Empty;
        string[] periods = closedPeriod.Split(" to ");

        if (periods.Length == 2)
        {
            startPeriod = periods[0];
            endPeriod = (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland) ? endDate.ToString("dd MMMM") : Resource.lbl31October;
            warningPeriod = $"{startPeriod} to {endPeriod}";
        }

        return (warningPeriod, startPeriod, endPeriod);
    }

    public (string, string) BindStartPeriodAndEndPeriod(string closedPeriod)
    {
        string startPeriod = string.Empty;
        string endPeriod = string.Empty;
        string[] periods = closedPeriod.Split(" to ");

        if (periods.Length == 2)
        {
            startPeriod = periods[0];
            endPeriod = periods[1];
        }

        return (startPeriod, endPeriod);
    }
    public async Task<(Error? error, decimal PreviousApplicationsNitrogen)> BindPreviousYearNitrogen(FertiliserManureViewModel model, int managementId, DateTime startDate, int fieldId, Error? error, DateTime endOfOctober, decimal PreviousApplicationsNitrogen)
    {
        if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
        {
            (PreviousApplicationsNitrogen, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endOfOctober, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
        }
        else
        {
            (PreviousApplicationsNitrogen, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, startDate, endOfOctober, null, false);
        }

        return (error, PreviousApplicationsNitrogen);
    }
    public async Task<(Error? error, decimal nitrogenInFourWeek)> BindNitrogenInFourWeekForWarning(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, DateTime fourWeekDate, decimal nitrogenInFourWeek)
    {
        if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
        {
            (nitrogenInFourWeek, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, fourWeekDate, model.Date.Value, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
        }
        else
        {
            (nitrogenInFourWeek, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, fourWeekDate, model.Date.Value, null, false);
        }

        return (error, nitrogenInFourWeek);
    }
    private async Task<(bool, WarningResponse, FertiliserManureViewModel)> BindResidueWarning(FertiliserManureViewModel model, decimal totalNitrogen, int managementId, bool isScotland, bool hasValidResidue, bool isNitrogenRateExceeded)
    {
        (bool isResidueGroupOne, bool isResidueGroupTwo, bool isResidueGroupThree, bool isResidueGroup4To6) = await BindResidueGroupCondtition(managementId, isScotland);
        WarningResponse warningResponse = new WarningResponse();
        if (hasValidResidue)
        {
            warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InOrgNMAXRateResidueGroup.ToString());
        }
        if (isResidueGroup4To6)
        {
            warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateResidueGroup4To6.ToString());
            isNitrogenRateExceeded = true;

        }
        bool isResidueGroupOneWithGreaterThan30 = isResidueGroupOne && (totalNitrogen > 30);
        bool isResidueGroupTwoWithGreaterThan20 = isResidueGroupTwo && (totalNitrogen > 20);
        bool isResidueGroupThreeWithGreaterThan10 = (isResidueGroupThree && (totalNitrogen > 10));
        if (isResidueGroupOneWithGreaterThan30)
        {
            isNitrogenRateExceeded = true;
            warningResponse.Para2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, 1, 30) : null;
        }
        else if (isResidueGroupTwoWithGreaterThan20)
        {
            isNitrogenRateExceeded = true;
            warningResponse.Para2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, 2, 20) : null;
        }
        else if (isResidueGroupThreeWithGreaterThan10)
        {
            isNitrogenRateExceeded = true;
            warningResponse.Para2 = !string.IsNullOrWhiteSpace(warningResponse.Para2) ? string.Format(warningResponse.Para2, 3, 10) : null;
        }

        if (isNitrogenRateExceeded)
        {

            model = SetClosedPeriodWarning(model, warningResponse, warningResponse.Para2);
        }

        return (isNitrogenRateExceeded, warningResponse, model);
    }
    public FertiliserManureViewModel SetClosedPeriodWarning(FertiliserManureViewModel model, WarningResponse warningResponse, string para2 = null)
    {
        model.IsNitrogenExceedWarning = true;
        model.ClosedPeriodNitrogenExceedWarningHeader = warningResponse.Header;
        model.ClosedPeriodNitrogenExceedWarningCodeID = warningResponse.WarningCodeID;
        model.ClosedPeriodNitrogenExceedWarningLevelID = warningResponse.WarningLevelID;
        model.ClosedPeriodNitrogenExceedWarningPara1 = warningResponse.Para1;
        model.ClosedPeriodNitrogenExceedWarningPara2 = para2;
        model.ClosedPeriodNitrogenExceedWarningPara3 = warningResponse.Para3;
        return model;
    }
    public async Task<(Error? error, decimal nitrogenWithinWarningPeriod)> BindNitrogenWithInWarningPeriod(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, DateTime start, DateTime end, decimal nitrogenWithinWarningPeriod)
    {
        if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
        {
            (nitrogenWithinWarningPeriod, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, start, end, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), false);
        }
        else
        {
            (nitrogenWithinWarningPeriod, error) = await FetchTotalNBasedOnFieldIdAndAppDate(fieldId, start, end, null, false);
        }

        return (error, nitrogenWithinWarningPeriod);
    }
    public async Task<(Error? error, decimal previousApplicationsN)> BindPreviousApplicationN(FertiliserManureViewModel model, int managementId, Error? error, int cropId, decimal previousApplicationsN)
    {
        if (model.UpdatedFertiliserIds != null && model.UpdatedFertiliserIds.Count > 0)
        {
            (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(cropId, false, model.UpdatedFertiliserIds.Where(x => x.ManagementPeriodId == managementId).Select(x => x.FertiliserId).FirstOrDefault(), null);
        }
        else
        {
            (previousApplicationsN, error) = await _organicManureLogic.FetchTotalNBasedOnCropIdFromOrgManureAndFertiliser(cropId, false, null, null);
        }

        return (error, previousApplicationsN);
    }

    public async Task<(Error? error, CropTypeLinkingResponse cropTypeLinking, int? scotlandNmax, int residueGroup, bool isWinterOilseedRapeAutumn)> BindDataForNmaxWarning(FertiliserManureViewModel model, int managementId, int fieldId, Error? error, int farmCountryId, int scotland, Crop? crop)
    {
        CropTypeLinkingResponse cropTypeLinking = new CropTypeLinkingResponse();
        int? scotlandNmax = null;
        int residueGroup = 0;
        bool isWinterOilseedRapeAutumn = false;
        if (farmCountryId == scotland)
        {
            Field field = await _fieldLogic.FetchFieldByFieldId(fieldId);
            (Recommendation? recommendation, error) = await _cropLogic.FetchRecommendationByManagementPeriodId(managementId);

            if (recommendation != null)
            {
                residueGroup = Convert.ToInt32(recommendation.NIndex);
            }
            isWinterOilseedRapeAutumn = Functions.IsWinterOilseedRapeAutumn(crop.CropTypeID ?? 0, model.HarvestYear ?? 0, model.Date.Value);
            (scotlandNmax, error) = await _organicManureLogic.FetchScotlandNmaxByCropIdSoilTypeIdAndResidueGroup(crop.CropTypeID.Value, isWinterOilseedRapeAutumn ? -1 : field.SoilTypeID ?? 0, residueGroup);
            if (scotlandNmax == null)
            {
                scotlandNmax = Convert.ToInt32(recommendation?.CropN);
            }
        }
        else
        {
            (cropTypeLinking, error) = await _organicManureLogic.FetchCropTypeLinkingByCropTypeId(crop.CropTypeID.Value);
        }

        return (error, cropTypeLinking, scotlandNmax, residueGroup, isWinterOilseedRapeAutumn);
    }

    public async Task<(FertiliserManureViewModel, WarningResponse)> BindNmaxWarningInModelForAsparagusAndOnionCrops(FertiliserManureViewModel model, int cropTypeId, decimal totalNitrogen, bool isWithinClosedPeriod, string startPeriod, string endPeriod)
    {
        WarningResponse warningResponse = new WarningResponse();
        bool isThisValidCrop = ((cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Commons.Enums.CropTypes.SaladOnions) && isWithinClosedPeriod);
        if (isThisValidCrop)
        {
            bool isNitrogenRateExceeded = false;
            int maxNitrogenRate = 0;
            bool isThisAsparagusCropWithNitrogen = (cropTypeId == (int)NMP.Commons.Enums.CropTypes.Asparagus && totalNitrogen > 50);
            if (isThisAsparagusCropWithNitrogen)
            {
                isNitrogenRateExceeded = true;
                maxNitrogenRate = 50;
            }

            bool isThisBulbOnionsCropWithNitrogen = (cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions && totalNitrogen > 40);
            if (isThisBulbOnionsCropWithNitrogen)
            {
                isNitrogenRateExceeded = true;
                maxNitrogenRate = 40;
            }

            bool isThisSaladOnionsCropWithNitrogen = (cropTypeId == (int)NMP.Commons.Enums.CropTypes.BulbOnions && totalNitrogen > 40);
            if (isThisSaladOnionsCropWithNitrogen)
            {
                isNitrogenRateExceeded = true;
                maxNitrogenRate = 40;
            }

            if (isNitrogenRateExceeded)
            {
                warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRate.ToString());

                model = SetClosedPeriodWarning(model, warningResponse, string.Format(warningResponse.Para2, startPeriod, endPeriod, maxNitrogenRate));
            }
        }

        return (model, warningResponse);
    }
    public async Task<(FertiliserManureViewModel, WarningResponse)> BindOilseedRapeWarnings(FertiliserManureViewModel model, int managementId, decimal totalNitrogen, string startPeriod, decimal PreviousApplicationsNitrogen, bool isWithinWarningPeriod, int cropTypeId)
    {
        bool isScotland = model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland;
        (bool isResidueGroupOne, bool isResidueGroupTwo, bool isResidueGroupThree, bool isResidueGroup4To6) = await BindResidueGroupCondtition(managementId, isScotland);

        bool hasValidResidue = isResidueGroupOne || isResidueGroupTwo || isResidueGroupThree;
        bool isOilseedRapeWarning = (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape
            && isWithinWarningPeriod
            && (!isScotland || hasValidResidue || isResidueGroup4To6));
        WarningResponse warningResponse = new WarningResponse();
        if (isOilseedRapeWarning)
        {
            bool isNitrogenRateExceeded = false;

            if (!isScotland)
            {
                bool isAppNitrogenIsGreaterThan30 = (PreviousApplicationsNitrogen + model.N.Value) > 30;
                isNitrogenRateExceeded = isAppNitrogenIsGreaterThan30;

                if (isNitrogenRateExceeded)
                {
                    warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateOSR.ToString());

                    model = SetClosedPeriodWarning(model, warningResponse, string.Format(warningResponse.Para2, startPeriod));
                }
            }
            else
            {
                (isNitrogenRateExceeded, warningResponse, model) = await BindResidueWarning(model, totalNitrogen, managementId, isScotland, hasValidResidue, isNitrogenRateExceeded);
            }
        }

        return (model, warningResponse);

    }
    private async Task<(bool isResidueGroupOne, bool isResidueGroupTwo, bool isResidueGroupThree, bool isResidueGroup4To6)> BindResidueGroupCondtition(int managementId, bool isScotland)
    {
        bool isResidueGroupOne = false;
        bool isResidueGroupTwo = false;
        bool isResidueGroupThree = false;
        bool isResidueGroup4To6 = false;
        Recommendation? recommendation = null;
        if (isScotland)
        {
            (recommendation, _) = await _cropLogic.FetchRecommendationByManagementPeriodId(managementId);
            if (recommendation != null && recommendation.NIndex == 1.ToString())
            {
                isResidueGroupOne = true;
            }
            if (recommendation != null && recommendation.NIndex == 2.ToString())
            {
                isResidueGroupTwo = true;
            }
            if (recommendation != null && recommendation.NIndex == 3.ToString())
            {
                isResidueGroupThree = true;
            }
            if (recommendation != null && (recommendation.NIndex == 4.ToString() || recommendation.NIndex == 5.ToString() || recommendation.NIndex == 6.ToString()))
            {
                isResidueGroup4To6 = true;
            }
        }

        return (isResidueGroupOne, isResidueGroupTwo, isResidueGroupThree, isResidueGroup4To6);
    }
    private static FertiliserManureViewModel SetNMaxWarning(FertiliserManureViewModel model, WarningResponse warningResponse, string para2)
    {
        model.IsNMaxLimitWarning = true;
        model.CropNmaxLimitWarningHeader = warningResponse.Header;
        model.CropNmaxLimitWarningCodeID = warningResponse.WarningCodeID;
        model.CropNmaxLimitWarningLevelID = warningResponse.WarningLevelID;
        model.CropNmaxLimitWarningPara1 = warningResponse.Para1;
        model.CropNmaxLimitWarningPara2 = para2;
        model.CropNmaxLimitWarningPara3 = warningResponse.Para3;
        return model;
    }
    public async Task<(bool flowControl, (FertiliserManureViewModel, Error?) value)> BindNmaxWarnings(FertiliserManureViewModel model, decimal totalNitrogenApplied, int farmCountryId, Crop crop, int? scotlandNmax, int? nmaxLimitEnglandOrWales, decimal nMaxLimit)
    {
        if (totalNitrogenApplied > nMaxLimit)
        {
            string cropTypeName = await _fieldLogic.FetchCropTypeById(crop.CropTypeID.Value);
            model.IsNMaxLimitWarning = true;

            bool isScotland = farmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland;
            WarningResponse warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(farmCountryId, NMP.Commons.Enums.WarningKey.NMaxLimit.ToString());
            if (!isScotland && crop.CropTypeID != null && (crop.CropTypeID.Value != (int)NMP.Commons.Enums.CropTypes.Grass || crop.SwardTypeID == (int)NMP.Commons.Enums.SwardType.Grass))
            {

                model = SetNMaxWarning(model, warningResponse, string.Format(warningResponse.Para2, cropTypeName, nmaxLimitEnglandOrWales, nMaxLimit));


            }
            if (isScotland)
            {
                SetNMaxWarning(model, warningResponse, string.Format(warningResponse.Para2, cropTypeName, scotlandNmax, nMaxLimit));
            }
        }


        return (flowControl: true, value: default);
    }
    public async Task<(FertiliserManureViewModel model, WarningResponse warningResponse, bool isNitrogenRateExceeded)> WarningForGrass(FertiliserManureViewModel model, WarningResponse warningResponse, string startPeriod, bool isNitrogenRateExceeded, decimal nitrogenWithinWarningPeriod)
    {
        bool isNitrogenGreterThan40Or80 = (model.N.Value > 40 || (nitrogenWithinWarningPeriod + model.N.Value) > 80);
        if (isNitrogenGreterThan40Or80)
        {
            isNitrogenRateExceeded = true;
        }

        if (isNitrogenRateExceeded)
        {
            warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateGrass.ToString());

            model = SetClosedPeriodWarning(model, warningResponse, string.Format(warningResponse.Para2, startPeriod));
        }

        return (model, warningResponse, isNitrogenRateExceeded);
    }
    public async Task<(FertiliserManureViewModel model, WarningResponse warningResponse)> WarningForBrassicaCrop(FertiliserManureViewModel model, decimal totalNitrogen, WarningResponse warningResponse, string startPeriod, string endPeriod, decimal nitrogenInFourWeek)
    {
        bool isClosedPeriodWarning = (model.FarmCountryId == (int)NMP.Commons.Enums.FarmCountry.Scotland ? totalNitrogen > 100 : (totalNitrogen > 100 || model.N.Value > 50 || nitrogenInFourWeek > 0));
        if (isClosedPeriodWarning)
        //nitrogenInFourWeek>0 means check Nitrogen applied within 28 days
        //totalNitrogen > 100 and brassica crop will work for Scotland as well
        {
            warningResponse = await _warningLogic.FetchWarningByCountryIdAndWarningKeyAsync(model.FarmCountryId ?? 0, NMP.Commons.Enums.WarningKey.InorgNMaxRateBrassica.ToString());

            model = SetClosedPeriodWarning(model, warningResponse, string.Format(warningResponse.Para2, startPeriod, endPeriod));
        }

        return (model, warningResponse);
    }
    public async Task<(FertiliserManureViewModel, List<Crop>)> HandleDefoliationList(FertiliserManureViewModel model)
    {
        if (model.DefoliationList != null && model.DefoliationList.Count > 0 && model.DefoliationCurrentCounter < model.DefoliationList.Count)
        {
            model.FieldID = model.DefoliationList[model.DefoliationCurrentCounter].FieldID;
            model.FieldName = model.DefoliationList[model.DefoliationCurrentCounter].FieldName;
        }
        List<Crop> cropList = new List<Crop>();
        string cropTypeName = string.Empty;
        bool isDefoliationListNeedToCreate = (model.DefoliationList == null || model.IsAnyChangeInField ||
        (model.DefoliationList != null && model.FertiliserManures.Where(x => x.IsGrass).Select(x => x.FieldID).Any(fieldId => !model.DefoliationList.Select(d => d.FieldID).Contains(fieldId.Value))));
        if (isDefoliationListNeedToCreate)
        {
            model.DefoliationList ??= new List<DefoliationList>();


            return (model, cropList);
        }

        return (model, cropList);
    }


}