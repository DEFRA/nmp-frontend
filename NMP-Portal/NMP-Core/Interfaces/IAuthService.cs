using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;
namespace NMP.Core.Interfaces;
public interface IAuthService
{
    Task<(int,Error)> AddOrUpdateUser(UserData userData);
}
