using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IUserFarmService : IService
    {
        Task<List<Farm>> UserFarmAsync(int userId);

    }
}
