using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FertiliserManureDataViewModel:FertiliserManure
    {
        public string? EncryptedId { get; set; }
        public string? EncryptedFieldName { get; set; }
    }
}
