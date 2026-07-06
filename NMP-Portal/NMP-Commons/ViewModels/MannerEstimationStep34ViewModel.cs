using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep34ViewModel
    {
        public int? UpdateNitrogenPriceQuestion { get; set; }
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblNitrogenmustNotExceedTwoDecimalplaces))]
        
        public decimal? NitrogenPrice { get; set; }
        public int? NitrogenProductPrice { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public int MannerEstimateId { get; set; } 
        public decimal? DefaultNutrintPrice { get; set; }
        public int? DefaultNitrogenProductPrice { get; set; }
        public bool IsComingFirstTime { get; set; } = false; 
        public int? NutrientProductId { get; set; }
        public string? NutrientProductName { get; set; } = string.Empty;
    }
}
