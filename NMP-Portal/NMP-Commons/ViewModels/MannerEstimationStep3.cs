using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep3
    {
        public string? Postcode { get; set; }
        public string FarmName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
    }
}
