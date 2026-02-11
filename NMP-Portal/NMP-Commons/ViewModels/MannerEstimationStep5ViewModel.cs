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
        public string FieldName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
    }
}
