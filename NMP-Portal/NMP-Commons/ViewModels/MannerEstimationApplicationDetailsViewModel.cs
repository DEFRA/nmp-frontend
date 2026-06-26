using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationApplicationDetailsViewModel:MannerEstimationApplication
    {
        public string ManureTypeName { get; set; } = string.Empty;
    }
}
