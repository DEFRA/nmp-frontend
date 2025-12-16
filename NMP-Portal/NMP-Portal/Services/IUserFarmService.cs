using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IUserFarmService : IService
    {
        Task<(UserFarmResponse, Error)> UserFarmAsync(int userId);

    }
}
