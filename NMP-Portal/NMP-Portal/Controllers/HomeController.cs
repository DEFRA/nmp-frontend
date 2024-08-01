using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using NMP.Portal.Resources;
using System.Diagnostics;
using System.Net.Http;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDataProtector _dataProtector;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        public HomeController(ILogger<HomeController> logger, IDataProtectionProvider dataProtectionProvider, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _dataProtector = dataProtectionProvider.CreateProtector("NMP.Portal.Controllers.HomeController");
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {            
            HttpClient client = _httpClientFactory.CreateClient("DefraIdentityConfiguration");
            var uri = new Uri($"{_configuration["CustomerIdentityInstance"]}{_configuration["CustomerIdentityDomain"]}/{_configuration["CustomerIdentityPolicyId"]}/v2.0/.well-known/openid-configuration");
            var response = await client.GetAsync(uri);
            if(response != null && response.IsSuccessStatusCode)
            {
                ViewBag.IsDefraCustomerIdentifyConfigurationWorking = response.IsSuccessStatusCode;
            }
            else
            {
                ViewBag.IsDefraCustomerIdentifyConfigurationWorking = false;
                ViewBag.Error = Resource.MsgDefraIdentityServiceDown;
            }
            

            return View();
        }                
    }
}
