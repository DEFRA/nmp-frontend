namespace NMP.Commons.Models;
public class MeasurementData
{
    public int CropTypeId { get; set; }
    public int? SeasonId { get; set; }
    public Step1ArablePotato? Step1ArablePotato { get; set; }
    public Step1Veg? Step1Veg { get; set; }
    public Step2? Step2 { get; set; }
    public Step3? Step3 { get; set; }
}
