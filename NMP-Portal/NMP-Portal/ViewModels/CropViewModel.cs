using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class CropViewModel:Crop
    {
        public bool isEnglishRules { get; set; }
        public string EncryptedFarmId { get; set; } = string.Empty;
        public int? CropGroupId { get; set; }
        public string? CropGroup { get; set; }
        public string? CropType { get; set; }
    }

   
}
