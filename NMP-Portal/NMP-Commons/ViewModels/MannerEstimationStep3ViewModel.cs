using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep3ViewModel
    {
        public string Postcode { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public bool IsPostCodeChange { get; set; } = false;
    }
}
