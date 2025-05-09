﻿using NMP.Portal.Resources;
using System;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class SoilAnalysis
    {
        //public int? ID { get; set; }
        public int? FieldID { get; set; }
        public int? Year { get; set; }
        public bool? SulphurDeficient { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblTheDate))]
        public DateTime? Date { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblSoilPHLevel))]
        [RegularExpression(@"^(?:9(?:\.[0-9])?|[0-8](?:\.[0-9])?)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterAPH))]
        public decimal? PH { get; set; }
        public int? PhosphorusMethodologyID { get; set; }

        [Range(0, 999, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForPhosphorus))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPhosphorusPerLitreOfSoil))]
        public int? Phosphorus { get; set; }

        [RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPhosphorusIndex))]
        public int? PhosphorusIndex { get; set; }

        [Range(0, 9998, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrient))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPotassiumPerLitreOfSoil))]
        public int? Potassium { get; set; }

        //[RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
       
        
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPotassiumIndex))]
        public int? PotassiumIndex { get; set; }
        
        [Range(0, 9998, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrient))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblMagnesiumPerLitreOfSoil))]
        public int? Magnesium { get; set; }

        [RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblMagnesiumIndex))]
        public int? MagnesiumIndex { get; set; }
        public int? SoilNitrogenSupply { get; set; }
        public int? SoilNitrogenSupplyIndex { get; set; }
        public DateTime? SoilNitrogenSampleDate { get; set; }
        public int? Sodium { get; set; }
        public decimal? Lime { get; set; }
        public string? PhosphorusStatus { get; set; }
        public string? PotassiumAnalysis { get; set; }

        public string? PotassiumStatus { get; set; }
        public string? MagnesiumAnalysis { get; set; }
        public string? MagnesiumStatus { get; set; }
        public string? NitrogenResidueGroup { get; set; }
        public string? Comments { get; set; }
        public int? PreviousID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
