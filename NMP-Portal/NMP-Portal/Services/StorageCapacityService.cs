namespace NMP.Portal.Services
{
    public class StorageCapacityService: Service,IStorageCapacityService
    {
        private readonly ILogger<StorageCapacityService> _logger;
        public StorageCapacityService(ILogger<StorageCapacityService> logger, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory) : base
        (httpContextAccessor, clientFactory)
        {
            _logger = logger;
        }


    }
}
