using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep6ViewModel
    {
        public bool IsWithinNVZ { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
        public string FieldName { get; set; } = string.Empty;
    }
}
