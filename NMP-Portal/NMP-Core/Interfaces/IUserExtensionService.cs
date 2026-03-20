using NMP.Commons.Models;
namespace NMP.Core.Interfaces;
public interface IUserExtensionService : IService
{
    Task<UserExtension?> FetchUserExtensionAsync();
    Task<UserExtension?> UpdateTermsOfUseAsync(TermsOfUse termsOfUse);
    Task<UserExtension?> UpdateShowAboutServiceAsync(AboutService aboutService);
    Task<UserExtension?> UpdateShowAboutMannerAsync(AboutManner aboutManner);
}
