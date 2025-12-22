using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NMP.Application;
using NMP.Commons.Resources;
namespace NMP.Portal.Controllers;
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHomeLogic _homeLogic;

    public HomeController(ILogger<HomeController> logger, IHomeLogic homeLogic)
    {
        _logger = logger;
        _homeLogic = homeLogic;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogTrace($"Home Controller : Index() action called");
        try
        {
            var isDefraCustomerIdentityWorking = await _homeLogic.IsDefraCustomerIdentifyConfigurationWorkingAsync();
            if (isDefraCustomerIdentityWorking)
            {
                ViewBag.IsDefraCustomerIdentifyConfigurationWorking = isDefraCustomerIdentityWorking;
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
            var isNmptServiceWorking = await _homeLogic.IsNmptServiceWorkingAsync();
            if (isNmptServiceWorking)
            {
                ViewBag.IsNmptServiceWorking = isNmptServiceWorking;
            }                
            else
            {
                ViewBag.IsNmptServiceWorking = false;
                ViewBag.ServiceError = Resource.MsgNmptServiceNotAvailable;
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ViewBag.IsNmptServiceWorking = false;
            ViewBag.ServiceError = Resource.MsgNmptApiServiceBlockedAccess;
        }
        catch (Exception)
        {
            ViewBag.IsNmptServiceWorking = false;
            ViewBag.ServiceError = Resource.MsgNmptServiceNotAvailable;
        }
        return View();
    }
}
