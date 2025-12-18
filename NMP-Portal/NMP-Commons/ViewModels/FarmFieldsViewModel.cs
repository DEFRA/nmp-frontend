using NMP.Commons.Models;
namespace NMP.Commons.ViewModels;
public class FarmFieldsViewModel
{
    public FarmFieldsViewModel()
    {
        Fields = new List<Field>();
    }
    public List<Field> Fields { get; set; }
    public string? FarmName { get; set; }
    public string? FieldName { get; set; }
    public string EncryptedFarmId { get; set; } = string.Empty;
}
