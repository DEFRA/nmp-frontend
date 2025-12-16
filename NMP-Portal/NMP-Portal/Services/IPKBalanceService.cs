using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IPKBalanceService : IService
    {
        Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year,int fieldId);        
    }
}
