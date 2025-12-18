namespace NMP.Commons.Models;
public class RecommendationComment
{
    public int? ID { get; set; }
    public int? RecommendationID { get; set; }
    public int? Nutrient { get; set; }
    public string? Comment { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? CreatedByID { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByID { get; set; }
    public int? PreviousID { get; set; }
}
