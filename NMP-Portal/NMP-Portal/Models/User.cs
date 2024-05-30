using Microsoft.AspNetCore.Identity;

namespace NMP.Portal.Models
{
    public class User:IdentityUser<int>
    {        
        public string GivenName { get; set; }= string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public Guid? UserIdentifier { get; set; }
    }
}
