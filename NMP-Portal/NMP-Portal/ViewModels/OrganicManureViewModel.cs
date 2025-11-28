using NMP.Portal.Models;
using NMP.Portal.Resources;
using NMP.Portal.ServiceResponses;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NMP.Portal.ViewModels
{
    public class OrganicManureViewModel
    {
        public OrganicManureViewModel()
        {
            ManureType = new ManureType();
        }
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public List<string>? FieldList { get; set; }

        public int? ManureGroupId { get; set; }
        public int? ManureTypeId { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblEnterTheDateInCorrectFormat))]
        public DateTime? ApplicationDate { get; set; }
        public string? DefaultNutrientValue { get; set; }
        public bool isEnglishRules { get; set; }

        public List<OrganicManureDataViewModel>? OrganicManures { get; set; }
        public ManureType ManureType { get; set; }

        public string? ManureGroupName { get; set; }
        public string? ManureTypeName { get; set; }

        public int? ApplicationRateMethod { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblApplicationRate))]
        [RegularExpression(@"^(?:0(\.\d{1})?|[1-9]{1}\d{0,2}(\.\d{1})?|250(\.0{1})?)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgForApplicationRate))]


        public decimal? ApplicationRate { get; set; }
        public int? ApplicationMethod { get; set; }
        //public int? ManualApplicationRate { get; set; }
        public decimal? Area { get; set; }
        public decimal? Quantity { get; set; }
        public int? ApplicationRateArable { get; set; }
        public int? IncorporationMethod { get; set; }
        public int? ApplicationMethodCount { get; set; }
        public int? IncorporationDelay { get; set; }
        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? DryMatterPercent { get; set; }
        public decimal? UricAcid { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }
        public bool? IsDefaultNutrient { get; set; } = false;
        public bool IsCheckAnswer { get; set; } = false;
        public int? AutumnCropNitrogenUptake { get; set; }
        public List<AutumnCropNitrogenUptakeDetail>? AutumnCropNitrogenUptakes { get; set; }
        public int? RainfallWithinSixHoursID { get; set; }
        public string? RainfallWithinSixHours { get; set; }
        public int? WindspeedID { get; set; }
        public string? Windspeed { get; set; }
        
        public bool IsFieldGroupChange { get; set; } = false;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsApplicationMethodChange { get; set; } = false;
        public bool IsIncorporationMethodChange { get; set; } = false;
        public bool IsDefaultNutrientOptionChange { get; set; } = false;
        public bool? IsManureTypeLiquid { get; set; }
        public string? ApplicationMethodName { get; set; }
        public string? IncorporationMethodName { get; set; }
        public string? IncorporationDelayName { get; set; }
        public string? FieldGroupName { get; set; }
        public string? CropTypeName { get; set; }
        public DateTime? SoilDrainageEndDate { get; set; }
        public int? TotalRainfall { get; set; }

        public string? FarmName { get; set; }
        public bool IsAnyNeedToStoreNutrientValueForFuture { get; set; } = false;

        public int? MoistureTypeId { get; set; }
        public string? MoistureType { get; set; }

        public int? RainWithin6Hours { get; set; }
        public bool IsComingFromRecommendation { get; set; } = false;
        public int? ManureGroupIdForFilter { get; set; }
        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsClosedPeriodWarning { get; set; } = false;
        public bool IsOrgManureNfieldLimitWarning { get; set; } = false;
        public bool IsNMaxLimitWarning { get; set; } = false;
        public bool IsEndClosedPeriodFebruaryWarning { get; set; } = false;
        public bool IsEndClosedPeriodFebruaryExistWithinThreeWeeks { get; set; } = false;
        //public bool IsClosedPeriodOrganicAppRateExceedMaxN { get; set; } = false;
        public bool IsStartPeriodEndFebOrganicAppRateExceedMaxN150 { get; set; } = false;
        public int? CropOrder { get; set; }
        public string? EncryptedFieldId { get; set; }
        public string? OtherMaterialName { get; set; }
        public DateTime? DefaultFarmManureValueDate { get; set; }
        public bool? IsThisDefaultValueOfRB209 { get; set; }
        public string? NmaxWarningHeading { get; set; }
        public string? NmaxWarningPara1 { get; set; }
        public string? NmaxWarningPara2 { get; set; }
        public string? CropNmaxLimitWarningHeading { get; set; }
        public string? CropNmaxLimitWarningPara1 { get; set; }
        public string? CropNmaxLimitWarningPara1Additional { get; set; }
        public string? CropNmaxLimitWarningPara2 { get; set; }
        public int? FarmCountryId { get; set; }
        public string? ClosedPeriod { get; set; } = string.Empty;
        public string? ClosedPeriodWarningHeading { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara1 { get; set; } = string.Empty;
        public string? ClosedPeriodWarningPara2 { get; set; } = string.Empty;
        public string? SlurryOrPoultryManureExistWithinLast20Days {  get; set; } = string.Empty;
        public bool? IsWithinClosedPeriod { get; set; }
        public string? EndClosedPeriodEndFebWarningHeading { get; set; } = string.Empty;
        public string? EndClosedPeriodEndFebWarningPara1 { get; set; } = string.Empty;
        public string? EndClosedPeriodEndFebWarningPara2 { get; set; } = string.Empty;

        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksHeading { get; set; } = string.Empty;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksPara1 { get; set; } = string.Empty;
        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksPara2 { get; set; } = string.Empty;
        public bool? HighReadilyAvailableNitrogen { get; set; }

        public string? StartClosedPeriodEndFebWarningHeading { get; set; } = string.Empty;
        public string? StartClosedPeriodEndFebWarningPara1 { get; set; } = string.Empty;
        public string? StartClosedPeriodEndFebWarningPara2 { get; set; } = string.Empty;
        public DateTime? ClosedPeriodStartDate { get; set; }
        public DateTime? ClosedPeriodEndDate { get; set; }
        public string? ClosedPeriodForUI { get; set; } = string.Empty;
        public bool? IsWithinNVZ { get; set; }
        public string? EncryptedOrgManureId { get; set; } = string.Empty;
        public List<FertiliserAndOrganicManureUpdateResponse>? UpdatedOrganicIds { get; set; }
        public string? FieldName { get; set; }
        public bool? IsApplicationDateChange { get; set; } = false;
        public bool? IsDeleteOrganic { get; set; }
        public bool? IsCancel { get; set; }
        public bool? Defoliation { get; set; }
        public List<DoubleCrop>? DoubleCrop { get; set; }
        public int DoubleCropCurrentCounter { get; set; }
        public string? DoubleCropEncryptedCounter { get; set; }
        public int? FieldID { get; set; }
        public bool IsDoubleCropAvailable { get; set; } = false;
        public bool IsAnyChangeInField { get; set; } = false;
        public bool IsAnyChangeInSameDefoliationFlag { get; set; } = false;
        public bool? IsSameDefoliationForAll { get; set; }
        public int DefoliationCurrentCounter { get; set; } = 0;
        public string? DefoliationEncryptedCounter { get; set; }
        public string? EncryptedCounter { get; set; }
        public bool? IsAnyCropIsGrass { get; set; }
        public int? GrassCropCount { get; set; }
        public bool NeedToShowSameDefoliationForAll { get; set; } = true;
        public bool IsDefaultValueChange { get; set; } = false;


        public string? NmaxWarningHeader { get; set; }
        public int NmaxWarningCodeID { get; set; }
        public int NmaxWarningLevelID { get; set; }

        public string? CropNmaxLimitWarningHeader { get; set; }
        public int CropNmaxLimitWarningCodeID { get; set; }
        public int CropNmaxLimitWarningLevelID { get; set; }

        public string? EndClosedPeriodEndFebWarningHeader { get; set; } = string.Empty;
        public int EndClosedPeriodEndFebWarningCodeID { get; set; }
        public int EndClosedPeriodEndFebWarningLevelID { get; set; }

        public string? ClosedPeriodWarningHeader { get; set; } = string.Empty;
        public int ClosedPeriodWarningCodeID { get; set; }
        public int ClosedPeriodWarningLevelID { get; set; }

        public string? EndClosedPeriodFebruaryExistWithinThreeWeeksHeader { get; set; } = string.Empty;
        public int EndClosedPeriodFebruaryExistWithinThreeWeeksCodeID { get; set; }
        public int EndClosedPeriodFebruaryExistWithinThreeWeeksLevelID { get; set; }

        public string? StartClosedPeriodEndFebWarningHeader { get; set; } = string.Empty;
        public int StartClosedPeriodEndFebWarningCodeID { get; set; }
        public int StartClosedPeriodEndFebWarningLevelID { get; set; }
    }
}
