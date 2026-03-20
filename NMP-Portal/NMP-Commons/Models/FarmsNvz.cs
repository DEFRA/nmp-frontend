using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class FarmsNvz
    {
        public int ID { get; set; }
        public int? FarmID { get; set; }
        public int NVZProgrammeID { get; set; }
        public string NVZProgrammeName { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
