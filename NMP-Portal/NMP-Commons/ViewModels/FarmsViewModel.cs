using NMP.Commons.Models;
namespace NMP.Commons.ViewModels;
public class FarmsViewModel
{
    public FarmsViewModel()
    {
        Farms = new List<Farm>();
    }
    public List<Farm> Farms { get; set; }
}
