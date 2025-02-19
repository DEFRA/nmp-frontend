using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Services
{
    public interface IUserExtensionService : IService
    {
        Task<(UserExtension, Error)> FetchUserExtensionAsync();
        Task<(UserExtension, Error)> UpdateTermsOfUseAsync(TermsOfUse termsOfUse);
        Task<(UserExtension, Error)> UpdateShowAboutServiceAsync(AboutService aboutService);
    }
}
