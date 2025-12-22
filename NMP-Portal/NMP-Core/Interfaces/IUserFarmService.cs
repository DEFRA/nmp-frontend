using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;

public interface IUserFarmService : IService
{
    Task<(UserFarmResponse, Error)> UserFarmAsync(int userId);
}
