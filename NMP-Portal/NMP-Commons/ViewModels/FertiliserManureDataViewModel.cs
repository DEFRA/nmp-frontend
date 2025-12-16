using NMP.Commons.Models;
namespace NMP.Commons.ViewModels;
public class FertiliserManureDataViewModel:FertiliserManure
{
    public string? EncryptedId { get; set; }
    public string? EncryptedFieldName { get; set; }
    public int? Defoliation { get; set; }
    public string? DefoliationName { get; set; }
    public string? FieldName { get; set; }
    public int? FieldID { get; set; }
    public string? EncryptedCounter { get; set; }
    public bool IsGrass { get; set; } = false;
}
