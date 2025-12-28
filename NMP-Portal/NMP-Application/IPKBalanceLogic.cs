using NMP.Commons.Models;

namespace NMP.Application;

public interface IPKBalanceLogic
{
    Task<PKBalance> FetchPKBalanceByYearAndFieldId(int year, int fieldId);
}
