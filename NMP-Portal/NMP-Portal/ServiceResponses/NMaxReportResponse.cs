using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class NMaxReportResponse
    {
        public string CropTypeName { get; set; } = string.Empty;
        public int NmaxLimit { get; set; }
        public bool IsComply { get; set; } = false;
        public string VegetableGroup { get; set; } = string.Empty;
        public List<NMaxLimitReportResponse> NMaxLimitReportResponse { get; set; }
        public List<NitrogenApplicationsForNMaxReportResponse> NitrogenApplicationsForNMaxReportResponse { get; set; }
    }
}
