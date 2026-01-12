using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;

public interface IFarmContextService : IService
{    
    Task<Farm?> FetchFarmByIdAsync(int farmId);    
}
