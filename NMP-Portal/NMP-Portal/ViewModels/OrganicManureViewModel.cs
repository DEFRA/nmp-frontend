using NMP.Portal.Models;
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
        public DateTime? ApplicationDate { get; set; }
        public bool? IsDefaultNutrientValues { get; set; }
        public int? ManureGroup { get; set; }
        public int? ManureTypeId { get; set; }
        public bool isEnglishRules { get; set; }

        public List<OrganicManure>? OrganicManures { get; set; }
        public ManureType ManureType { get; set; }

        public string? ManureGroupName { get; set; }
        public string? ManureTypeName { get; set; }


    }
}
