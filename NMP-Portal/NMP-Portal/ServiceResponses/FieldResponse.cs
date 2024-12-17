using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class FieldResponse
    {
        public Field Field { get; set; }
        public SoilAnalysis? SoilAnalysis { get; set; }
        public SnsAnalysis? SnsAnalyses { get; set; }
        public Crop? Crop { get; set; }
        public List<PreviousGrass>? PreviousGrasses { get; set; }
    }
}
