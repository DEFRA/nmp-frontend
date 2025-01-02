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

        public CropAndFieldReportResponse? CropAndFieldReport { get; set; }
        public List<NutrientResponseWrapper>? Nutrients { get; set; }
    }
}
