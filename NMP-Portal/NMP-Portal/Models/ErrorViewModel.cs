using NMP.Portal.ServiceResponses;

namespace NMP.Portal.Models
{
    public class ErrorViewModel: Error
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string StatusCode { get; set; }
    }
}
