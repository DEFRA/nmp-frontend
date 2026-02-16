using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep3ViewModel
    {
        [StringLength(8, MinimumLength = 6, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]
        [RegularExpression(@"^[A-Za-z]{1,2}\d{1,2}[A-Za-z]?\s*\d[A-Za-z]{2}$", ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgPostcodeMinMaxValidation))]

        public string Postcode { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public bool IsPostCodeChange { get; set; } = false;
    }
}
