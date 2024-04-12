namespace NMP.Portal.Services
{
    public class AuthService: Service
    {
        public AuthService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory): base(httpContextAccessor, clientFactory)
        {
            
        }
    }
}
