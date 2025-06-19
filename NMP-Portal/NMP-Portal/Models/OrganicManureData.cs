using Newtonsoft.Json;

namespace NMP.Portal.Models
{
    public class OrganicManureData:OrganicManure
    {
        public string ManureTypeName { get; set; }
        public string ApplicationMethodName { get; set; } = string.Empty;
        public string? IncorporationMethodName { get; set; } = string.Empty;
        public string? IncorporationDelayName { get; set; } = string.Empty;
        public string? EncryptedId { get; set; }
        public string? EncryptedFieldName { get; set; }
        public string? EncryptedManureTypeName { get; set; }
        public string? RateUnit { get; set; }
    }
}
