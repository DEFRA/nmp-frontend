using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Application;

public interface IFarmLogic
{
    Task<Farm?> FetchFarmByIdAsync(int farmId);
}
