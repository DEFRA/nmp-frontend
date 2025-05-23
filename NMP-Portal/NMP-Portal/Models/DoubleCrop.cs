namespace NMP.Portal.Models
{
    public class DoubleCrop
    {
        public int CropID { get; set; }
        public string CropName { get; set; } = string.Empty;
        public int CropOrder { get; set; }
        public int FieldID { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public int Counter { get; set; }
        public string EncryptedCounter { get; set; } = string.Empty;
    }
}
