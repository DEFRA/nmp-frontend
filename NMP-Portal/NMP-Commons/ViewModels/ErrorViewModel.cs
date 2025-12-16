namespace NMP.Commons.ViewModels;

//TODO: Need to revisit this class later
public class ErrorViewModel         //: Error
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string StatusCode { get; set; }
}
