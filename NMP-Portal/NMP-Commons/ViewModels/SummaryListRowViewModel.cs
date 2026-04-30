using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class SummaryListRowViewModel
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? DefaultValue { get; set; }
        public string? ActionName { get; set; }
        public string? ControllerName { get; set; }
        public  bool ShowData{ get; set; } = true;
        public bool ShowAction { get; set; } = true;
        public bool IsGrassYearsRow { get; set; } = false;

    }
}
