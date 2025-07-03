using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IReportService
    {
        Task<(NutrientsLoadingFarmDetail, Error)> AddNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
        Task<(NutrientsLoadingFarmDetail, Error)> FetchNutrientsLoadingFarmDetailsByFarmIdAndYearAsync(int farmId,int year);
        Task<(NutrientsLoadingFarmDetail, Error)> UpdateNutrientsLoadingFarmDetailsAsync(NutrientsLoadingFarmDetail nutrientsLoadingFarmDetailsData);
    }
}
