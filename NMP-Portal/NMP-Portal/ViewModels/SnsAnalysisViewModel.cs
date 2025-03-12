using NMP.Portal.Models;
using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels
{
    public class SnsAnalysisViewModel:SnsAnalysis
    {
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblFieldName))]
        public string? FieldName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public string? EncryptedFarmId { get; set; } = string.Empty;
        public string? EncryptedFieldId { get; set; } = string.Empty;
        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? EncryptedCropId { get; set; } = string.Empty;
        public int CropId { get; set; } 
        public string? EncryptedFieldName { get; set; } = string.Empty;
        public int? CropTypeId { get; set; }
        public int? SoilMineralNitrogenAt030CM { get; set; }
        public int? SoilMineralNitrogenAt3060CM { get; set; }
        public int? SoilMineralNitrogenAt6090CM { get; set; }
        public int? SampleDepth { get; set; }
        public int? SoilMineralNitrogen { get; set; }
        public bool? IsCalculateNitrogen { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheNumberOfShootsPerSquareMetre))]
        public int? NumberOfShoots { get; set; }
        public int SeasonId { get; set; }
        public bool? IsEstimateOfNitrogenMineralisation { get; set; }
        public bool? IsBasedOnSoilOrganicMatter { get; set; }
        public int GreenAreaIndexOrCropHeight { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheCropHeight))]
        public decimal? CropHeight { get; set; }

        [RegularExpression(@"^(?:0(\.\d{1,2})?|[1-2]?\d(\.\d{1,2})?|3(\.0{1,2})?)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgForGreenAreaIndex))]
        public decimal? GreenAreaIndex { get; set; }

        public bool IsCropHeight { get; set; } = false;
        public bool IsGreenAreaIndex { get; set; } = false;
        public bool IsNumberOfShoots { get; set; } = false;
        public bool IsCalculateNitrogenNo { get; set; } = false;
        public decimal? SoilOrganicMatter { get; set; }
        public decimal? AdjustmentValue { get; set; }
        public int SnsIndex { get; set; }
        public int SnsValue { get; set; }
        public int? SnsCategoryId { get; set; }
        public bool? RecentSoilAnalysisQuestion { get; set; }
        public bool IsRecentSoilAnalysisQuestionChange { get; set; } = false;
    }
}
