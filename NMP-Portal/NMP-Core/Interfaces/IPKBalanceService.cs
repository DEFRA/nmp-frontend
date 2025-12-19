using NMP.Commons.Models;
namespace NMP.Core.Interfaces;
public interface IPKBalanceService : IService
{
    Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year,int fieldId);        
}
