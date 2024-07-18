﻿using NMP.Portal.Models;
using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

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
        public bool? IsDefaultNutrientValues { get; set; }
        public bool isEnglishRules { get; set; }

        public List<OrganicManure>? OrganicManures { get; set; }
        public ManureType ManureType { get; set; }

        public string? ManureGroupName { get; set; }
        public string? ManureTypeName { get; set; }

        public int? ApplicationRateMethod { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblApplicationRate))]
        public int? ApplicationRate { get; set; }
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
        public decimal? AutumnCropNitrogenUptake { get; set; }
        public int? RainfallWithinSixHoursID { get; set; }
        public string? RainfallWithinSixHours { get; set; }
        public int? WindspeedID { get; set; }
        public string? Windspeed { get; set; }
        
        public bool IsFieldGroupChange { get; set; } = false;
        public bool IsManureTypeChange { get; set; } = false;
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
    }
}
