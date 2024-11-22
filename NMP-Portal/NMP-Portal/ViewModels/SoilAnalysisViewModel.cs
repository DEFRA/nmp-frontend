using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class SoilAnalysisViewModel : SoilAnalysis
    {
        public string FarmName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;

        public string EncryptedFieldId { get; set; } = string.Empty;
        public string EncryptedSoilAnalysisId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }
        public string IsSoilDataChanged { get; set; } = string.Empty;

        public PKBalance? PKBalance { get; set; }
    }
}
