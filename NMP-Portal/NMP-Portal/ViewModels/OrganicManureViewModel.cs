using NMP.Portal.Models;
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
        //public string? ManureGroup { get; set; }
        public int? ManureTypeId { get; set; }
        // public string? ManureType { get; set; }
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblEnterTheDateInCorrectFormat))]
        public DateTime? ApplicationDate { get; set; }
        public bool? IsDefaultNutrientValues { get; set; }
        public bool isEnglishRules { get; set; }

        public List<OrganicManure>? OrganicManures { get; set; }
        public ManureType ManureType { get; set; }

        public string? ManureGroupName { get; set; }
        public string? ManureTypeName { get; set; }

        public int? ApplicationRateMethod { get; set; }
        public decimal? ApplicationRate { get; set; }
        public int? ApplicationMethod { get; set; }
        //public int? ManualApplicationRate { get; set; }
        public decimal? Area { get; set; }
        public decimal? Quantity { get; set; }
        public int? ApplicationRateArable { get; set; }
        public int? IncorporationMethod { get; set; }
        public int? ApplicationMethodCount { get; set; }
    }
}
