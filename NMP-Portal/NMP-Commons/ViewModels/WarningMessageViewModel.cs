using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class WarningMessageViewModel
    {
        public bool IsWales { get; set; }

        public List<WarningMessage> Warnings { get; set; }
            = new List<WarningMessage>();
    }
}
