using NMP.Commons.Models;

namespace NMP.Application;

public interface IFarmLogic
{
    Task<Farm?> FetchFarmByIdAsync(int farmId);
}
