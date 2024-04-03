﻿using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class Farm
    {
        public int Id { get; set; } = 0;
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblWhatIsTheFarmName))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheFarmName))]
        public string Name { get; set; } = string.Empty;
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Address4 { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblWhatIsTheFarmPostcode))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhatIsTheFarmPostcode))]
        public string PostCode { get; set; } = string.Empty;
        public string? CPH { get; set; }
        public string? FarmerName { get; set; }
        public string? BusinessName { get; set; }
        public string? SBI { get; set; }
        public string? STD { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public int? Rainfall { get; set; }
        public decimal TotalFarmArea { get; set; } = 0;
        public int AverageAltitude { get; set; } = 0;
        public bool RegistredOrganicProducer { get; set; } = false;
        public bool MetricUnits { get; set; } = false;
        public bool EnglishRules { get; set; } = true;
        public bool NVZField { get; set; } = false;
        public bool FieldsAbove300SeaLevel { get; set; } = false;
        public string? EncryptedFarmName { get; set; } 
        public string? EncryptedPostcode { get; set; }
    }
}
