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

           

           
            public DateTime CreatedOn { get; set; }
            public int CreatedByID { get; set; }

            public DateTime? ModifiedOn { get; set; }
            public int? ModifiedByID { get; set; }
        
    }
}
