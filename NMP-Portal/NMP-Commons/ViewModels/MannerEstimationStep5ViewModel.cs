using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep5ViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheFieldName))]
        [StringLength(50, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgFieldNameMaxLengthValidation))]
        public string FieldName { get; set; } = string.Empty;
        public string? EncryptedMannerEstimateId { get; set; } = string.Empty;
        public int? FarmId { get; set; }
    }
}
