using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IAuthService
    {
        Task<(int,Error)> AddOrUpdateUser(UserData userData);
    }
}
