using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{

    public class FieldViewModel : Field
    {
        public FieldViewModel()
        {
            SoilAnalyses=new SoilAnalysis();
            Crops=new List<Crop>();
            Fields = new List<Field>();
            ManagementPeriods = new List<ManagementPeriod>();
        }
        public List<Field> Fields { get; set; }
        public List<ManagementPeriod> ManagementPeriods { get; set; }
        public bool IsSoilReleasingClay { get; set; } = false;
        public SoilAnalysis SoilAnalyses { get; set; }
        public List<Crop> Crops { get; set; }
        public int? CropTypeID { get; set; }
        public string? FarmName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }
        public bool? IsSnsBasedOnPreviousCrop { get; set; }
        public int? CropGroupId { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public string? SoilType { get; set; } = string.Empty;
        public string? CropType { get; set; } = string.Empty;
        public string? CropGroup { get; set; } = string.Empty;
        public string SampleDate { get; set; } = string.Empty;
        public bool isEnglishRules { get; set; } 

        public int FarmID { get; set; }
        public bool? IsWithinNVZForFarm { get; set; }
        public bool? IsAbove300SeaLevelForFarm { get; set; }
        public int? LastHarvestYear { get; set; }

    }
}
