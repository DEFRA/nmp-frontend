using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface IMannerEstimationServiceDependencies
    {
        IMannerEstimationService MannerEstimationService { get; }
        IMannerService MannerService { get; }
        IFieldService FieldService { get; }
        IFarmService FarmService { get; }
        ICropService CropService { get; }
    }
}
