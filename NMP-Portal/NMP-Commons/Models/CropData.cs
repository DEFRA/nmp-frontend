namespace NMP.Commons.Models;
public class CropData
{
    public CropData()
    {         
        ManagementPeriods = new List<ManagementPeriod>();
        Crop = new Crop();
    }
    public Crop Crop { get; set; }
    public List<ManagementPeriod> ManagementPeriods { get; set; }
}
