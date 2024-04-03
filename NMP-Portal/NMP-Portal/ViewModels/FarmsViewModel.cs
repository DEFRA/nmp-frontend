using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
    
{
    public class FarmsViewModel
    {
        public FarmsViewModel()
        {
            Farms = new List<Farm>();
        }
        public List<Farm> Farms { get; set; }
    }
}
