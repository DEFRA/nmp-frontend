using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NMP.Portal.Resources;

namespace NMP.Portal.Models
{
    public class FarmAddress
    {
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblAddressLine1))]
        public string Address1 { get; set; } = string.Empty;

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblAddressLine2))]
        public string? Address2 { get; set; } = string.Empty;

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblTownOrCity))]
        public string? Address3 { get; set; } = string.Empty;

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblCounty))]
        public string? Address4 { get; set; } = string.Empty;

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblPostCode))]
        public string PostCode { get; set; } = string.Empty;
        
    }
}
