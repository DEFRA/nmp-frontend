namespace NMP.Portal.Models
{
    public class UserExtension
    {
        public UserExtension()
        {
            IsTermsOfUseAccepted = false;
            DoNotShowAboutThisService = false;
        }
        public int UserId { get; set; }
        public bool IsTermsOfUseAccepted { get; set; }
        public bool DoNotShowAboutThisService { get; set; }

    }
}
