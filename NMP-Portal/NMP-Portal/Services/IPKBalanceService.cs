using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IPKBalanceService : IService
    {
        Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year,int fieldId);
        Task<(PKBalance,Error Error)> AddPKBalance(string pkBalanceData);
        Task<(PKBalance, Error Error)> UpdatePKBalance(string pkBalanceData);
    }
}
