using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FertiliserManureViewModel
    {
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public string? FarmName { get; set; }

        public bool isEnglishRules { get; set; }

        public List<string>? FieldList { get; set; }
        public bool IsComingFromRecommendation { get; set; } = false;
        public bool IsCheckAnswer { get; set; } = false;
        public string? FieldGroupName { get; set; }

        public string? CropTypeName { get; set; }
        public string? InOrgnaicManureDuration { get; set; }
        public List<FertiliserManure>? FertiliserManures { get; set; }

        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? Na2O { get; set; }
        public decimal? Lime { get; set; }
        public DateTime? Date { get; set; }
        public bool? QuestionForSpreadInorganicFertiliser { get; set; }
        public string? FieldName { get; set; }
        public RecommendationViewModel? RecommendationViewModel { get; set; }
        public string? EncryptedCounter { get; set; }
        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsClosedPeriodWarningExceptGrassAndOilseed { get; set; } = false;
        public bool IsClosedPeriodWarningOnlyForGrassAndOilseed { get; set; } = false;
        public bool IsNitrogenExceedWarning { get; set; } = false;
        public int? CropOrder { get; set; }
        public int? FarmCountryId { get; set; }
        public bool IsClosedPeriodWarning { get; set; } = false;
        public string? ClosedPeriodWarningHeading { get; set; } = string.Empty;
        //public string? ClosedPeriodWarningPara1 { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara2 { get; set; } = string.Empty;

        public string? ClosedPeriodNitrogenExceedWarningHeading { get; set; } = string.Empty;
        public string? ClosedPeriodNitrogenExceedWarningPara1 { get; set; } = string.Empty;
        public string? ClosedPeriodNitrogenExceedWarningPara2 { get; set; } = string.Empty;
        public bool? IsWithinNVZ { get; set; }
        public string? EncryptedIsUpdate { get; set; } = string.Empty;
    }
}
