using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep34ViewModel
    {
        public int? UpdateNutrientPriceQuestion { get; set; }
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblNitrogenmustNotExceedTwoDecimalplaces))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhichHarvestWouldYouLikeToPlanFor))]
        public decimal? NitrogenPrice { get; set; }
        public int? NitrogenProductPrice { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
    }
}
