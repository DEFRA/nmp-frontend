using NMP.Commons.Models;
using NMP.Commons.ServiceResponses;

namespace NMP.Commons.ViewModels;
public class FertiliserManureViewModel
{
    public string? FieldGroup { get; set; }
    public string? EncryptedFarmId { get; set; }
    public string? EncryptedHarvestYear { get; set; }
    public string? CropGroupName { get; set; }
    public int? FarmId { get; set; }
    public int? HarvestYear { get; set; }
    public string? FarmName { get; set; }
    public bool isEnglishRules { get; set; }
    public List<string>? FieldList { get; set; }
    public bool IsComingFromRecommendation { get; set; } = false;
    public bool IsCheckAnswer { get; set; } = false;        
    public string? InOrgnaicManureDuration { get; set; }
    public List<FertiliserManureDataViewModel>? FertiliserManures { get; set; }
    public int? N { get; set; }
    public int? P2O5 { get; set; }
    public int? K2O { get; set; }
    public int? MgO { get; set; }
    public int? SO3 { get; set; }
    public int? Na2O { get; set; }
    public decimal? Lime { get; set; }
    public DateTime? Date { get; set; }
    public bool? QuestionForSpreadInorganicFertiliser { get; set; }
    public string? FieldName { get; set; }
    public Recommendation? Recommendation { get; set; }
    public string? EncryptedCounter { get; set; }
    public bool IsWarningMsgNeedToShow { get; set; } = false;
    public bool IsClosedPeriodWarningExceptGrassAndOilseed { get; set; } = false;
    public bool IsClosedPeriodWarningOnlyForGrassAndOilseed { get; set; } = false;
    public bool IsNitrogenExceedWarning { get; set; } = false;
    public int? CropOrder { get; set; }
    public int? FarmCountryId { get; set; }
    public bool IsClosedPeriodWarning { get; set; } = false;
    public string? ClosedPeriodWarningHeading { get; set; } = string.Empty;        
    public string? ClosedPeriodWarningPara2 { get; set; } = string.Empty;
    public string? ClosedPeriodNitrogenExceedWarningHeading { get; set; } = string.Empty;
    public string? ClosedPeriodNitrogenExceedWarningPara1 { get; set; } = string.Empty;
    public string? ClosedPeriodNitrogenExceedWarningPara2 { get; set; } = string.Empty;
    public bool? IsWithinNVZ { get; set; }
    public string? EncryptedFertId { get; set; } = string.Empty;       
    public List<FertiliserAndOrganicManureUpdateResponse>? UpdatedFertiliserIds { get; set; }
    public bool? IsDeleteFertliser { get; set; }
    public bool? IsCancel { get; set; }
    public int? Defoliation { get; set; }
    public bool? IsAnyCropIsGrass { get; set; }
    public int DefoliationCurrentCounter { get; set; } = 0;
    public string? DefoliationEncryptedCounter { get; set; }
    public int? FieldID { get; set; }
    public bool? IsSameDefoliationForAll { get; set; }
    public bool IsCropGroupChange { get; set; } = false;
    public bool IsAnyChangeInField { get; set; } = false;
    public int? GrassCropCount { get; set; }
    public bool IsAnyChangeInSameDefoliationFlag { get; set; } = false;
    public List<DoubleCrop>? DoubleCrop { get; set; }
    public int DoubleCropCurrentCounter { get; set; }
    public string? DoubleCropEncryptedCounter { get; set; }
    public bool IsDoubleCropAvailable { get; set; } = false;
    public bool NeedToShowSameDefoliationForAll { get; set; } = true;
    public string? ClosedPeriodWarningHeader { get; set; } = string.Empty;
    public int ClosedPeriodWarningCodeID { get; set; }
    public int ClosedPeriodWarningLevelID { get; set; }
    public string? ClosedPeriodNitrogenExceedWarningHeader { get; set; } = string.Empty;
    public int ClosedPeriodNitrogenExceedWarningCodeID { get; set; }
    public int ClosedPeriodNitrogenExceedWarningLevelID { get; set; }
    public List<DefoliationList>? DefoliationList { get; set; }
    public bool IsDoubleCropValueChange { get; set; } = false;
}
