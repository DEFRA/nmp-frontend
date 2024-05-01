using NMP.Portal.Resources;
using System;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class SoilAnalyses
    {
        //public int? ID { get; set; }
        //public int? FieldID { get; set; }
        public int? Year { get; set; }
        public bool? SulphurDeficient { get; set; }

//        [Required(AllowEmptyStrings =false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheDateInNumber))]
        [DataType(DataType.DateTime, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheDateInNumber))]
        //[RegularExpression(@"^(0?[1-9]|[12][0-9]|3[01]) (0?[1-9]|1[0-2]) \d{4}$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheDateInNumber))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblDateSampleTaken))]
        public DateTime? Date { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblSoilPHLevel))]
        [RegularExpression(@"^(?:9(?:\.[0-9])?|[0-8](?:\.[0-9])?)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterAPH))]
        public decimal? PH { get; set; }
        public int? PhosphorusMethodologyID { get; set; }

        [RegularExpression(@"^(?:[0-9]{1,4}|10000)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrient))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPhosphorusPerLitreOfSoil))]
        public int? Phosphorus { get; set; }

        [RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPhosphorusIndex))]
        public int? PhosphorusIndex { get; set; }

        [RegularExpression(@"^(?:[0-9]{1,4}|10000)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrient))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPotassiumPerLitreOfSoil))]
        public int? Potassium { get; set; }

        [RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPotassiumIndex))]
        public int? PotassiumIndex { get; set; }

        [RegularExpression(@"^(?:[0-9]{1,4}|10000)$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrient))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblMagnesiumPerLitreOfSoil))]
        public int? Magnesium { get; set; }

        [RegularExpression(@"^[0-9]$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterValidValueForNutrientIndex))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblMagnesiumIndex))]
        public int? MagnesiumIndex { get; set; }
        public int? SoilNitrogenSupply { get; set; }
        public int? SoilNitrogenSupplyIndex { get; set; }
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
