namespace NMP.Commons.Models;
public class CropDataWrapper
{
    public CropDataWrapper()
    {
        Crops = new List<CropData>();
    }

    public List<CropData> Crops { get; set; }
}
