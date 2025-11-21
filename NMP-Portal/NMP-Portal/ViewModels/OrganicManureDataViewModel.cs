using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class OrganicManureDataViewModel:OrganicManure
    {
        public string ApplicationMethodName { get; set; } = string.Empty;
        public string? IncorporationMethodName { get; set; } = string.Empty;
        public string? IncorporationDelayName { get; set; } = string.Empty;
        public string? EncryptedId { get; set; }
        public string? EncryptedFieldName { get; set; }
        public string? EncryptedManureTypeName { get; set; }
        public string? RateUnit { get; set; }
        public int? Defoliation { get; set; }
        public string? DefoliationName { get; set; }
        public int? FieldID { get; set; }
        public string? FieldName { get; set; }
        public string? EncryptedCounter { get; set; }
    }
}
