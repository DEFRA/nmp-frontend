using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IAuthService
    {
        Task<(int,Error)> AddOrUpdateUser(UserData userData);
    }
}
