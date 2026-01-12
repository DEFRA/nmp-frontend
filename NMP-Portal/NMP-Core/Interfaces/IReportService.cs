using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;
namespace NMP.Core.Interfaces;
public interface IReportService
{
    Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId,int year);
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

}
