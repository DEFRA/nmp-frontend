﻿using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class Farm
    {
        public int Id { get; set; }
        //[Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterTheFarmName))]
        [StringLength(250,ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgFarmNameMinMaxValidation))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheFarmName))]
        public string? Name { get; set; }
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblSelectTheFarmAddress))]
        public string? FullAddress { get; set; } = string.Empty;

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblAddressLine1))]
        public string? Address1 { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblAddressLine2))]
        public string? Address2 { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblTownOrCity))]
        public string? Address3 { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblCounty))]
        public string? Address4 { get; set; }

        [StringLength(8, MinimumLength = 6 ,ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]
        [RegularExpression(@"^[A-Z]{1,2}\d{1,2}[A-Z]?\s*\d[A-Z]{2}$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheFarmPostcode))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheFarmPostcode))]
        public string PostCode { get; set; }=string.Empty;
        public string? CPH { get; set; }
        public string? FarmerName { get; set; }
        public string? BusinessName { get; set; }
        public string? SBI { get; set; }
        public string? STD { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        [RegularExpression("^[0-9]+$",  ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheAverageAnnualRainfall))] 
        public int? Rainfall { get; set; }
        public decimal TotalFarmArea { get; set; } = 0;
        public int AverageAltitude { get; set; } = 0;
        public bool? RegistredOrganicProducer { get; set; } = false;
        public bool MetricUnits { get; set; } = false;
        public bool EnglishRules { get; set; } = true;
        public int? NVZField { get; set; } = 0;
        public int? FieldsAbove300SeaLevel { get; set; } = 0;        
    }
}
