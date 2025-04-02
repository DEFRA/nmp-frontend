using NMP.Portal.Models;
using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

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
            PKBalance = new PKBalance();
            PreviousGrasses = new PreviousGrass();
        }
        public List<Field> Fields { get; set; }
        public List<ManagementPeriod> ManagementPeriods { get; set; }
        public bool IsSoilReleasingClay { get; set; } = false;
        public SoilAnalysis SoilAnalyses { get; set; }
        public PreviousGrass PreviousGrasses { get; set; }
        public List<Crop> Crops { get; set; }
        public PKBalance PKBalance { get; set; }
        public int? CropTypeID { get; set; }
        public string? FarmName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }
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
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblSampleForSoilMineralNitrogen))]
        public DateTime? SampleForSoilMineralNitrogen { get; set; }

        public bool? RecentSoilAnalysisQuestion { get; set; }
        public bool IsRecentSoilAnalysisQuestionChange { get; set; } = false;
        //public bool SoilOverChalk { get; set; } = false;
        public string? EncryptedIsUpdate { get; set; } = string.Empty;
        public bool? FieldRemove { get; set; }
        public List<int>? PreviousGrassYears { get; set; }
        public bool? CopyExistingField { get; set; }
        public bool? IsPreviousYearGrass { get; set; }
        public string? PreviousCrop { get; set; }
        public int? PreviousCropID { get; set; }
        public string? Management { get; set; }
        public string? PotassiumIndexValue { get; set; }
        public string? CropGroupName { get; set; }
        public int? HarvestYear { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public bool IsHasGrassInLastThreeYearChange { get; set; } = false;
    }
}
