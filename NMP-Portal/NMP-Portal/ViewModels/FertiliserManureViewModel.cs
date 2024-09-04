﻿using NMP.Portal.Models;

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
        public int? InOrgnaicManureDurationId { get; set; }
        public string? InOrgnaicManureDuration { get; set; }
        public List<FertiliserManure>? FertiliserManures { get; set; }

        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? Na2O { get; set; }
        public decimal? Lime { get; set; }
        public bool? QuestionForSpreadInorganicFertiliser { get; set; }
        public string? FieldName { get; set; }
        public RecommendationViewModel? RecommendationViewModel { get; set; }
        public string? EncryptedCounter { get; set; }
        public int? Counter { get; set; }
        public List<ApplicationForFertiliserManure>? ApplicationForFertiliserManures { get; set; }
        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsClosedPeriodWarningExceptGrassAndOilseed { get; set; } = false;
        public bool IsClosedPeriodWarningOnlyForGrassAndOilseed { get; set; } = false;
        public bool IsNitrogenExceedWarning { get; set; } = false;
    }
}
