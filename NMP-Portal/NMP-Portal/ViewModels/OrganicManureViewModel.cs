using NMP.Portal.Models;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ViewModels
{
    public class OrganicManureViewModel : OrganicManure
    {
        public string? FieldGroup { get; set; }
        public string? EncryptedFarmId { get; set; }
        public string? EncryptedHarvestYear { get; set; }
        public int? FarmId { get; set; }
        public int? HarvestYear { get; set; }
        public List<string>? FieldList { get; set; }
        public DateTime ApplicationDate { get; set; }
        public bool IsDefaultNutrientValues { get; set; }
    }
    
}
