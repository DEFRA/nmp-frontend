using NMP.Portal.Models;
using NMP.Portal.ServiceResponses;

namespace NMP.Portal.ViewModels
{
    public class ReportViewModel
    {
        public List<string>? FieldList { get; set; }
        public string EncryptedFarmId { get; set; } = string.Empty;
        public string? EncryptedHarvestYear { get; set; } = string.Empty;
        public string? LastModifiedOn { get; set; }
        public int? Year { get; set; }
        public int? FarmId { get; set; }
        public string? FarmName { get; set; } = string.Empty;

        public int? ReportType { get; set; }
        public CropAndFieldReportResponse? CropAndFieldReport { get; set; }
        public List<NutrientResponseWrapper>? Nutrients { get; set; }
        public List<string>? CropTypeList { get; set; }
        public Farm? Farm { get; set; }
    }
}
