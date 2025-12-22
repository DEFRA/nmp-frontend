using NMP.Commons.Models;

namespace NMP.Application;

public interface IFarmContextLogic
{
    Task<Farm?> FetchFarmByIdAsync(int farmId);
}
