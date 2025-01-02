namespace NMP.Portal.Models
{
    public class OrganicManureData:OrganicManure
    {
        public string ManureTypeName { get; set; }
        public string ApplicationMethodName { get; set; } = string.Empty;
        public string? IncorporationMethodName { get; set; } = string.Empty;
        public string? IncorporationDelayName { get; set; } = string.Empty;
    }
}
