using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep1ViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheFarmName))]
        [StringLength(250, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgFarmNameMinMaxValidation))]
        public string FarmName { get; set; } = string.Empty;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsFarmCopied { get; set; } = false;
        public int? FarmId { get; set; }
    }
}
