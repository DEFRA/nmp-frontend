using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep2
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MsgSelectACountryBeforContinuing))]
        public int CountryID { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public string FarmName { get; set; } = string.Empty;
    }
}
