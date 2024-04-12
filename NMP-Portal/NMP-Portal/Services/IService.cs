namespace NMP.Portal.Services
{
    public interface IService
    {
        Task<HttpResponseMessage> PostJsonDataAsync(string url, object? model = null);
        Task<HttpResponseMessage> GetDataAsync(string url);
    }
}
