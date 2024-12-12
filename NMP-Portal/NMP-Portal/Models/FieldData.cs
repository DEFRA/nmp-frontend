namespace NMP.Portal.Models
{
    public class FieldData
    {
        public Field Field { get; set; }
        public SoilAnalysis SoilAnalysis { get; set; }
        public SnsAnalysis? SnsAnalysis { get; set; }
        public List<CropData> Crops { get; set; }
        public PKBalance PKBalance { get; set; }
        public List<PreviousGrass> PreviousGrasses { get; set; }
    }
}
