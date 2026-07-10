using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep39ViewModel
    {
        public int? UpdatePotashPriceQuestion { get; set; }
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPotashPrceMustNotExceedTwoDecimal))]        
        public decimal? PotashPrice { get; set; }
        public int? PotashProductPrice { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public int MannerEstimateId { get; set; }
        public bool IsComingFirstTime { get; set; } = false;
        public int? NutrientProductId { get; set; }
        public string? NutrientProductName { get; set; } = string.Empty;
    }
}
