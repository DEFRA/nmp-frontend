using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class SoilAnalysisViewModel : SoilAnalysis
    {
        public string? FarmName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string EncryptedFarmId { get; set; } = string.Empty;

        public string EncryptedFieldId { get; set; } = string.Empty;
        public string? EncryptedSoilAnalysisId { get; set; } = string.Empty;
        public bool? IsSoilNutrientValueTypeIndex { get; set; }
        public string IsSoilDataChanged { get; set; } = string.Empty;

        public PKBalance? PKBalance { get; set; }
        public bool? isSoilAnalysisAdded { get; set; }
        public string? PotassiumIndexValue { get; set; } = string.Empty;
        public string? PhosphorusMethodology { get; set; } = string.Empty;
        public bool? SoilAnalysisRemove { get; set; }
        public bool? IsCancel { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
    }
}