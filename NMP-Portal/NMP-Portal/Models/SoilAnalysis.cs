using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class SoilAnalysis
    {
        public int? ID { get; set; }
        public int? FieldID { get; set; }
        public int? Year { get; set; }
        public bool? SulphurDeficient { get; set; }
        public DateTime? Date { get; set; }
        public decimal? PH { get; set; }
        public int? PhosphorusMethodologyId { get; set; }
        public int? Phosphorus { get; set; }
        public int? PhosphorusIndex { get; set; }
        public int? Potassium { get; set; }

        public int? PotassiumIndex { get; set; }
        public int? Magnesium { get; set; }
        public int? MagnesiumIndex { get; set; }
        public int? SoilNitrogenSupply { get; set; }
        public int? SoilNitrogenSupplyIndex { get; set; }
        public int? Sodium { get; set; }
        public decimal? Lime { get; set; }
        public string? PhosphorusStatus { get; set; }
        public string? PotassiumAnalysis { get; set; }

        public string? PotassiumStatus { get; set; }
        public string? MagnesiumAnalysis { get; set; }
        public string? MagnesiumStatus { get; set; }
        public string? NitrogenResidueGroup { get; set; }
        public string? Comments { get; set; }
    }
}
