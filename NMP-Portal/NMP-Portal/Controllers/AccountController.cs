using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NMP.Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;
using NMP.Portal.Helpers;
using Azure.Core;
using Microsoft.Extensions.Options;

namespace NMP.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {

        private readonly IConfiguration _configuration;
        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Logout()
        {
            var accessToken = HttpContext.User.FindFirst("access_token").Value;

            
            

            using (var client = new HttpClient())
            {

                // The Token Endpoint Authentication Method must be set to POST if you
                // want to send the client_secret in the POST body.
                // If Token Endpoint Authentication Method then client_secret must be
                // combined with client_id and provided as a base64 encoded string
                // in a basic authorization header.
                // e.g. Authorization: basic <base64 encoded ("client_id:client_secret")>
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", accessToken),
                    new KeyValuePair<string, string>("token_type_hint", "access_token"),
                    new KeyValuePair<string, string>("client_id", _configuration["CustomerIdentityClientId"]),
                    new KeyValuePair<string, string>("client_secret", _configuration["CustomerIdentityClientSecret"])
                });

                var uri = new Uri("https://your-account.cpdev.cui.defra.gov.uk/idphub/b2c/b2c_1a_cui_cpdev_signupsignin/signout");

                //var uri = String.Format("https://{0}.onelogin.com/oidc/token/revocation", options.Value.Region);

                var res = await client.PostAsync(uri, formData);
                var json = await res.Content.ReadAsStringAsync();
                bool success = res.IsSuccessStatusCode;

                base.SignOut();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            HttpContext?.Session.Clear();
            return RedirectToAction("Index", "Home");

            // return RedirectToAction("SignOut", "Account", new { Area = "MicrosoftIdentity" });
        }
    }
}
