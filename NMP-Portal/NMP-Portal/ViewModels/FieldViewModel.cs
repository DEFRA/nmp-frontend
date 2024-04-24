using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{

    public class FieldViewModel : Field
    {
        public SoilAnalysis SoilAnalysis { get; set; }

        public FieldViewModel()
        {
            SoilAnalysis=new SoilAnalysis();
        }

        public bool IsSoilReleasingClay { get; set; } = false;
        public string EncryptedFarmId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }

    }
}
