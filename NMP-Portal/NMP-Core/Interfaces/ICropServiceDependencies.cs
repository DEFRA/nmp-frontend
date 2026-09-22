using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Core.Interfaces
{
    public interface ICropServiceDependencies
    {
        ICropService CropService { get; }
        ISnsAnalysisService SnsAnalysisService { get; }
        IRecommendationService RecommendationService { get; }
        IRb209Service Rb209Service { get; }
    }
}
