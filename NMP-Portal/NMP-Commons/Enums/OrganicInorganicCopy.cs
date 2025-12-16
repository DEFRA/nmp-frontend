namespace NMP.Commons.Enums;

[Flags]
public enum OrganicInorganicCopy
{
    None = 0,
    OrganicMaterial =1,
    InorganicFertiliser=2,
    Both = OrganicMaterial | InorganicFertiliser

}
