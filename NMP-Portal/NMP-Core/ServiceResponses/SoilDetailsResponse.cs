namespace NMP.Core.ServiceResponses;
public class SoilDetailsResponse
{
    public int? SoilTypeId { get; set; }
    public string? SoilTypeName { get; set; }
    public bool? PotashReleasingClay { get; set; }
    public bool? SulphurDeficient { get; set; }
    public int? StartingP { get; set; }
    public int? StartingK { get; set; }
}
