using NMP.Commons.Models;
using NMP.Commons.ViewModels;

namespace NMP.Application;

public interface IAcceptTermsLogic
{
    Task<bool> IsUserTermsOfUseAccepted();    
    Task<UserExtension?> UpdateTermsOfUseAsync(TermsOfUseViewModel model);
}
