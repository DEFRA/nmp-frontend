using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface IReportLogic
{
    Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId, int year);
    Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmId(int farmId);
    Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure);
    Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmId(int farmId);
    Task<(List<CommonResponse>, Error)> FetchLivestockGroupList();
    Task<(CommonResponse, Error)> FetchLivestockGroupById(int livestockGroupId);
    Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id);
    Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupId(int livestockGroupId);
    Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId);
    Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);
    Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYear(int farmId, int year);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypes();
    Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id);
    Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId);
    Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);
    Task<(
            int soilTypeAdjustment,
            int millingWheat,
            decimal yieldAdjustment,
            int paperCrumbleOrStrawMulch,
            decimal grassCut)>
        BindAdjustmentsForEnglandAndWales(Crop crop, Field field, int year);
    Task<(int yieldAdjustment, int marketAdjustment, int rainfallAdjustment)> BindAdjustmentsForScotland(Crop crop,int? winterRainfall,int? nResidueGroup,int soilTypeId,decimal? standardYield, List<ScotlandNMaxValue>? scotlandNMaxValue,int farmId);
    Task<(List<FieldDetails>, decimal, int, int)>
        BindNmaxResponseForScotland(
            ReportViewModel model,
            Crop crop,
            Field field,
            List<FieldDetails> fieldDetail,
            decimal? defaultYield, string previousCrop, List<ScotlandNMaxValue>? scotlandNMaxValue);
    Task<(decimal?, decimal?)> FetchTotalNitroegen(List<ManagementPeriod> ManPeriodList, bool isAutumn);
    Task ProcessEnglandAndWales(
       Crop crop,
       Field field,
       ReportViewModel model,
       HarvestYearPlanResponse cropData,
        string cropTypeName,
        int nMaxLimitForCropType,
       List<NMaxLimitReportResponse> nMaxList);
    Task ProcessScotland(
         ReportViewModel model,
         HarvestYearPlanResponse cropData,
         List<FieldDetails> fieldDetail,
          int nMaxLimitForCropType,
         List<NMaxLimitReportResponse> nMaxList,
         string previousCrop,
         List<ScotlandNMaxValue>? scotlandNMaxValue);
     Task<string> GetPreviousCropAsync(int fieldId, int year);
    Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropId(int cropId);
}
