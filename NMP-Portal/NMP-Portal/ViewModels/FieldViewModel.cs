using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{

    public class FieldViewModel : Field
    {
        public FieldViewModel()
        {
            SoilAnalysis=new SoilAnalysis();
            Crop=new Crop();
        }

        public bool IsSoilReleasingClay { get; set; } = false;
        public SoilAnalysis SoilAnalysis { get; set; }
        public Crop Crop { get; set; }
        public string? FarmName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }
        public bool? IsSnsBasedOnPreviousCrop { get; set; }
        public int? CropGroupId { get; set; }
    }
}
