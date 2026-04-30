namespace NMP.Commons.Models;
public class FarmContext
{
    public FarmContext()
    {
        EncryptedFarmId = string.Empty;
        FarmName = string.Empty;
    }

    public int FarmId { get; set; }
    public string EncryptedFarmId { get; set; }
    public string FarmName { get; set; }
}
