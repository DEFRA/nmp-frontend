using NMP.Commons.Resources;
using System.ComponentModel.DataAnnotations;
namespace NMP.Commons.Models;
public class StoreCapacity
{
    public int? ID { get; set; }
    public int? FarmID { get; set; }
    public int? Year { get; set; }
    public string? StoreName { get; set; }
    public int? MaterialStateID { get; set; }
    public int? StorageTypeID { get; set; }
    public int? SolidManureTypeID { get; set; }  //not in view model

    [Range(
    typeof(decimal),
    "0",
    "999",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And999)
)]
    [RegularExpression(@"^\d+(\.\d{1})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterTheLengthToOneDecimalPlace))]
    
    public decimal? Length { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "999",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And999))]
    [RegularExpression(@"^\d+(\.\d{1})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterTheWidthToOneDecimalPlace))]
    public decimal? Width { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "99",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And99))]
    [RegularExpression(@"^\d+(\.\d{1})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterTheDepthToOneDecimalPlace))]
    public decimal? Depth { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "999",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And999))]
    public decimal? Circumference { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "999",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And999))]
    [RegularExpression(@"^\d+(\.\d{1})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterTheDiameterToOneDecimalPlace))]
    public decimal? Diameter { get; set; }
    public int? BankSlopeAngleID { get; set; }
    public bool? IsCovered { get; set; }
    public decimal? CapacityVolume { get; set; }

    [Range(
    typeof(decimal),
    "0",
    "9999999999",
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And9999999999))]
    public decimal? CapacityWeight { get; set; }
    public decimal? SurfaceArea { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? CreatedByID { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedByID { get; set; }
}
