using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
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
            _logger.LogTrace($"Home Controller : Index() action called");
            try
            {
                HttpClient client = _httpClientFactory.CreateClient("DefraIdentityConfiguration");
                var uri = new Uri($"{_configuration["CustomerIdentityInstance"]}{_configuration["CustomerIdentityDomain"]}/{_configuration["CustomerIdentityPolicyId"]}/v2.0/.well-known/openid-configuration");
                var response = await client.GetAsync(uri);
                if (response != null && response.IsSuccessStatusCode)
                {
                    ViewBag.IsDefraCustomerIdentifyConfigurationWorking = response.IsSuccessStatusCode;
                }
                else
                {
                    ViewBag.IsDefraCustomerIdentifyConfigurationWorking = false;
                    ViewBag.Error = Resource.MsgDefraIdentityServiceDown;
                }


            }
            catch (Exception ex)
            {
                ViewBag.IsDefraCustomerIdentifyConfigurationWorking = false;
                ViewBag.Error = Resource.MsgDefraIdentityServiceDown;
            }
            try
            {
                HttpClient nmptServiceClient = _httpClientFactory.CreateClient("NMPApi");
                var serviceresponse = await nmptServiceClient.GetAsync(new Uri($"{_configuration["NMPApiUrl"]}"));
                if (serviceresponse != null && serviceresponse.IsSuccessStatusCode)
                {
                    ViewBag.IsNmptServiceWorking = serviceresponse.IsSuccessStatusCode;
                }
                else if (serviceresponse != null && serviceresponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    ViewBag.IsNmptServiceWorking = false;
                    ViewBag.ServiceError = Resource.MsgNmptApiServiceBlockedAccess;
                }
                else
                {
                    ViewBag.IsNmptServiceWorking = false;
                    ViewBag.ServiceError = Resource.MsgNmptServiceNotAvailable;
                }
            }
            catch (Exception ex)
            {
                ViewBag.IsNmptServiceWorking = false;
                ViewBag.ServiceError = Resource.MsgNmptServiceNotAvailable;
            }
            return View();
        }
    }
}
