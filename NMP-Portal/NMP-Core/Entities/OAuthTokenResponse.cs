using Newtonsoft.Json;

namespace NMP.Core;
public class OAuthTokenResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonProperty("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonProperty("expires_in")]
    public string ExpiresIn { get; set; } = string.Empty;

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonProperty("refresh_token_expires_in")]
    public string RefreshTokenExpiresIn { get; set; } = string.Empty;

    [JsonProperty("expires_on")]
    public string ExpiresOn { get; set; } = string.Empty;
}
