using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IReportService
{
    Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId,int year);
    Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmIdAsync(int farmId);
    Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresAsync(string nutrientsLoadingManure);
    Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAsync(int farmId);
    Task<(List<CommonResponse>, Error)> FetchLivestockGroupListAsync();
    Task<(CommonResponse, Error)> FetchLivestockGroupByIdAsync(int livestockGroupId);
    Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdAsync(int id);
    Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresAsync(string nutrientsLoadingManure);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupIdAsync(int livestockGroupId);
    Task<(string, Error)> DeleteNutrientsLoadingManureByIdAsync(int nutrientsLoadingManureId);

    Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);

    Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYearAsync(int farmId, int year);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesAsync();
    Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdAsync(int id);
    Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdAsync(int nutrientsLoadingLivestockId);

    Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);
    Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropIdAsync(int cropId);
    Task<(List<ReportYearLastUpdatedDateResponse>, Error?)> FetchLastUpdatedDateByFarmIdAndYearAsync(int farmId, string years);
}
