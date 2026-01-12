namespace NMP.Commons.Models
{
    public class FieldData
    {
        public Field Field { get; set; }
        public SoilAnalysis SoilAnalysis { get; set; }        
        public PKBalance PKBalance { get; set; }
        public List<PreviousCroppingData> PreviousCroppings { get; set; }
    }
}
