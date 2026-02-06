namespace NMP.Commons.Models;
public class UserExtension
{
    public UserExtension()
    {
        IsTermsOfUseAccepted = false;
        DoNotShowAboutThisService = false;
        DoNotShowAboutManner = false;
    }
    public int UserId { get; set; }
    public bool IsTermsOfUseAccepted { get; set; }
    public bool DoNotShowAboutThisService { get; set; }
    public bool DoNotShowAboutManner { get; set; }
}
