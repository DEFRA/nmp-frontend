using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class MannerEstimationFinancialValues
    {
            public int Id { get; set; }
            public int MannerEstimationApplicationID { get; set; }

            public int NitrogenValue { get; set; }
            public int PhosphateValue { get; set; }
            public int PotashValue { get; set; }

            public int NitrogenProductId { get; set; }
            public int PhosphateProductId { get; set; }
            public int PotashProductId { get; set; }


        public string NitrogenProductName { get; set; } = string.Empty;
            public string PhosphateProductName { get; set; } = string.Empty;
        public string PotashProductName { get; set; } = string.Empty;

        public int NitrogenProductPrice { get; set; }
            public int PhosphateProductPrice { get; set; }
            public int PotashProductPrice { get; set; }

            public int NitrogenPrice { get; set; }
            public int PhosphatePrice { get; set; }
            public int PotashPrice { get; set; }

            public DateTime CreatedOn { get; set; }
            public int CreatedByID { get; set; }

            public DateTime? ModifiedOn { get; set; }
            public int? ModifiedByID { get; set; }
        
    }
}
