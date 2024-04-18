using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FieldViewModel : Field
    {
        public string FarmName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;

    }
}
