using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class FarmViewModel:Farm
    {
        public FarmViewModel()
        {

        }
        public bool IsManualAddress { get; set; } = false;
        public bool IsCheckAnswer { get; set; } = false;
        public string? OldPostcode { get; set; }
        
    }
}
