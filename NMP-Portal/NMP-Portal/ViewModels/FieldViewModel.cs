using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FieldViewModel : Field
    {
        public FieldViewModel()
        {
            SoilAnalysis = new SoilAnalysis();
        }
        public string FarmName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;
        public SoilAnalysis SoilAnalysis { get; set; }
    }
}
