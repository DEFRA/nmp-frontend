using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
using NMP.Core.Interfaces;

namespace NMP.Application;

public interface IReportLogic: IReportService
{
    
    Task<(
            int soilTypeAdjustment,
            int millingWheat,
            decimal yieldAdjustment,
            int paperCrumbleOrStrawMulch,
            decimal grassCut)>
        BindAdjustmentsForEnglandAndWales(Crop crop, Field field, int year);
    Task<(int yieldAdjustment, int marketAdjustment, int rainfallAdjustment)> BindAdjustmentsForScotland(ScotlandAdjustmentContext ctx);
    Task<(List<FieldDetails>, decimal, int, int)> BindNmaxResponseForScotland(BindNmaxResponseForScotlandContext ctx);
    Task<(decimal?, decimal?)> FetchTotalNitroegen(List<ManagementPeriod> manPeriodList, bool isAutumn);
    Task ProcessEnglandAndWales(
       Crop crop,
       Field field,
       ReportViewModel model,
       HarvestYearPlanResponse cropData,
        string cropTypeName,
        int nMaxLimitForCropType,
       List<NMaxLimitReportResponse> nMaxList);
    Task ProcessScotland(ProcessScotlandContext ctx);
     Task<string> GetPreviousCropAsync(int fieldId, int year);
    Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropId(int cropId);
}
