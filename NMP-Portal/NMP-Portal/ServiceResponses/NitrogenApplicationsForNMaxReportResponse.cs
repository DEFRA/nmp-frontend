using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class NitrogenApplicationsForNMaxReportResponse
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string CropTypeName { get; set; } = string.Empty;
        public decimal CropArea { get; set; }
        public decimal? InorganicNRate { get; set; }
        public decimal? InorganicNTotal { get; set; }
        public decimal? OrganicCropAvailableNRate { get; set; }
        public decimal? OrganicCropAvailableNTotal { get; set; }
        public decimal? NRate { get; set; }
        public decimal? NTotal { get; set; }
    }
}
