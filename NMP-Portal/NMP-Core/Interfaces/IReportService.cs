using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IReportService
{
    Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsServiceAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearServiceAsync(int farmId,int year);
    Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsServiceAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(List<NutrientsLoadingManures>, Error)> FetchNutrientsLoadingManuresByFarmIdServiceAsync(int farmId);
    Task<(NutrientsLoadingManures, Error)> AddNutrientsLoadingManuresServiceAsync(string nutrientsLoadingManure);
    Task<(List<NutrientsLoadingFarmDetail>, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdServiceAsync(int farmId);
    Task<(List<CommonResponse>, Error)> FetchLivestockGroupListServiceAsync();
    Task<(CommonResponse, Error)> FetchLivestockGroupByIdServiceAsync(int livestockGroupId);
    Task<(NutrientsLoadingManures, Error)> FetchNutrientsLoadingManuresByIdServiceAsync(int id);
    Task<(NutrientsLoadingManures, Error)> UpdateNutrientsLoadingManuresServiceAsync(string nutrientsLoadingManure);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesByGroupIdServiceAsync(int livestockGroupId);
    Task<(string, Error)> DeleteNutrientsLoadingManureByIdServiceAsync(int nutrientsLoadingManureId);

    Task<(NutrientsLoadingLiveStock, Error)> AddNutrientsLoadingLiveStockServiceAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);

    Task<(List<NutrientsLoadingLiveStockViewModel>, Error)> FetchLivestockByFarmIdAndYearServiceAsync(int farmId, int year);
    Task<(List<LivestockTypeResponse>, Error)> FetchLivestockTypesServiceAsync();
    Task<(NutrientsLoadingLiveStock, Error)> FetchNutrientsLoadingLiveStockByIdServiceAsync(int id);
    Task<(string, Error)> DeleteNutrientsLoadingLivestockByIdServiceAsync(int nutrientsLoadingLivestockId);

    Task<(NutrientsLoadingLiveStock, Error)> UpdateNutrientsLoadingLiveStockServiceAsync(NutrientsLoadingLiveStock nutrientsLoadingLiveStockData);
    Task<(OrganicManureFertiliserResponse, Error?)> FetchOrganicManureFertiliserByCropIdServiceAsync(int cropId);
}
