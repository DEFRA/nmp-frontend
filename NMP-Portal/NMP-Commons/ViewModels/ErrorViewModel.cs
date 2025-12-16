using NMP.Commons.ServiceResponses;
namespace NMP.Commons.ViewModels;

public class ErrorViewModel : Error
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string StatusCode { get; set; }
}
