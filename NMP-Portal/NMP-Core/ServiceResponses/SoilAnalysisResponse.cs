using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class SoilAnalysisResponse
{
    [JsonProperty("id")]
    public int? ID { get; set; }

    [JsonProperty("fieldID")]
    public int? FieldID { get; set; }

    [JsonProperty("year")]
    public int? Year { get; set; }

    [JsonProperty("sulphurDeficient")]
    public bool? SulphurDeficient { get; set; }

    [JsonProperty("date")]
    public DateTime? Date { get; set; }

    [JsonProperty("pH")]
    public decimal? PH { get; set; }

    [JsonProperty("phosphorusMethodologyID")]
    public int? PhosphorusMethodologyID { get; set; }
    public string? PhosphorusMethodology { get; set; }

    [JsonProperty("phosphorus")]
    public int? Phosphorus { get; set; }

    [JsonProperty("phosphorusIndex")]
    public int? PhosphorusIndex { get; set; }

    [JsonProperty("potassium")]
    public int? Potassium { get; set; }

    [JsonProperty("potassiumIndex")]
    public int? PotassiumIndex { get; set; }

    [JsonProperty("magnesium")]
    public int? Magnesium { get; set; }

    [JsonProperty("magnesiumIndex")]
    public int? MagnesiumIndex { get; set; }

    [JsonProperty("soilNitrogenSupply")]
    public int? SoilNitrogenSupply { get; set; }

    [JsonProperty("soilNitrogenSupplyIndex")]
    public int? SoilNitrogenSupplyIndex { get; set; }

    [JsonProperty("sodium")]
    public int? Sodium { get; set; }

    [JsonProperty("lime")]
    public decimal? Lime { get; set; }

    [JsonProperty("phosphorusStatus")]
    public string? PhosphorusStatus { get; set; }

    [JsonProperty("potassiumAnalysis")]
    public string? PotassiumAnalysis { get; set; }

    [JsonProperty("potassiumStatus")]
    public string? PotassiumStatus { get; set; }

    [JsonProperty("magnesiumAnalysis")]
    public string? MagnesiumAnalysis { get; set; }

    [JsonProperty("magnesiumStatus")]
    public string? MagnesiumStatus { get; set; }

    [JsonProperty("nitrogenResidueGroup")]
    public string? NitrogenResidueGroup { get; set; }
    public string? PotassiumIndexValue { get; set; }

    [JsonProperty("comments")]
    public string? Comments { get; set; }

    [JsonProperty("previousID")]
    public int? PreviousID { get; set; }

    [JsonProperty("createdOn")]
    public DateTime? CreatedOn { get; set; }

    [JsonProperty("createdByID")]
    public int? CreatedByID { get; set; }

    [JsonProperty("modifiedOn")]
    public DateTime? ModifiedOn { get; set; }

    [JsonProperty("modifiedByID")]
    public int? ModifiedByID { get; set; }
    public string? EncryptedSoilAnalysisId { get; set; }
}
