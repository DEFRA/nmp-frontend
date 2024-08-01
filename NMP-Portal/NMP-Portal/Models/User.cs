using Microsoft.AspNetCore.Identity;

namespace NMP.Portal.Models
{
    public class User
    {   
        public int ID { get; set; }
        public string GivenName { get; set; }= string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public Guid? UserIdentifier { get; set; }
    }
}
