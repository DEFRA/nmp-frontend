using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NMP.Commons.ServiceResponses;
using NMP.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using NMP.Commons.Helpers;
namespace NMP.Services;
public abstract class Service(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, TokenRefreshService tokenRefresh) : IService
{
    public readonly IHttpClientFactory _clientFactory = clientFactory;
    public readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly TokenRefreshService _tokenRefreshService = tokenRefresh;

    private async Task<string> GetAccessTokenAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
        {
            return null;
        }

        var token = await ctx.GetTokenAsync("access_token");
        return token;
    }

    private static bool JwtExpired(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        return token.ValidTo < DateTime.UtcNow.AddMinutes(5);
    }

    public async Task<HttpClient> GetNMPAPIClient()
    {
        var accessToken = await GetAccessTokenAsync();

        if (JwtExpired(accessToken))
        {
            accessToken = await _tokenRefreshService.RefreshUserAccessTokenAsync(context: _httpContextAccessor.HttpContext);
        }

        HttpClient httpClient = _clientFactory.CreateClient("NMPApi");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return await Task.FromResult(httpClient).ConfigureAwait(false);

    }

    public async Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.PostAsJsonAsync(url, model);
        return response;
    }

    public async Task<HttpResponseMessage> GetDataAsync(string url)
    {
        HttpClient httpClient = await GetNMPAPIClient();
        var response = await httpClient.GetAsync(url);
        return response;
    }

    protected async Task<(T?, Error)> SendRequestAsync<T>(
    Func<HttpClient, Task<HttpResponseMessage>> httpCall,
    Func<ResponseWrapper?, T?> mapData,
    ILogger logger)
    {
        Error error = new();
        T? resultData = default;

        try
        {
            HttpClient httpClient = await GetNMPAPIClient();
            var response = await httpCall(httpClient);

            string result = await response.Content.ReadAsStringAsync();
            ResponseWrapper? responseWrapper =
                JsonConvert.DeserializeObject<ResponseWrapper>(result);

            if (response.IsSuccessStatusCode)
            {
                resultData = mapData(responseWrapper);
            }
            else
            {
                logger.ExtractError(responseWrapper, error);
            }
        }
        catch (HttpRequestException hre)
        {
            logger.HandleHttpRequestException(hre, error);
        }
        catch (Exception ex)
        {
            logger.HandleException(ex, error);
        }

        return (resultData, error);
    }
}
