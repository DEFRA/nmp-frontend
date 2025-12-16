using NMP.Commons.Models;
namespace NMP.Commons.ViewModels;
public class FarmViewModel:Farm
{
    public FarmViewModel()
    {

    }
    public string? Country { get; set; }
    public bool IsManualAddress { get; set; } = false;
    public bool IsCheckAnswer { get; set; } = false;
    public bool IsPostCodeChanged { get; set; } = false;
    public bool IsPlanExist { get; set; } = false;
    public string? EncryptedIsUpdate { get; set; } = string.Empty;
    public bool? FarmRemove { get; set; }
    public int? ArableArea { get; set; }
    public int? GrassArea { get; set; }
    public bool? IsCancel { get; set; }
}
