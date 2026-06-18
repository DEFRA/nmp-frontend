using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep31ViewModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgEnterTheName))]
        [StringLength(250, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgNameMinMaxValidation))]
        public string Name { get; set; } = string.Empty;
        public bool? IsCopyEstimate { get; set; } 
    }
}
