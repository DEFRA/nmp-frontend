using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep2ViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgSelectACountryBeforContinuing))]
        public int CountryID { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public int? FarmRB209CountryId { get; set; }
        public bool IsCountryIdChange { get; set; } = false;
    }
}
