namespace NMP.Portal.Models
{
    [Serializable]
    public class Token
    {
        public string AccessToken { get; set; }= string.Empty;
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Issues { get; set; } = string.Empty;
        public string Expires { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }
}
