using Microsoft.AspNetCore.Authorization;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using System.Net;

namespace NMP.Portal.Authorization
{
    public class ValidateTokenHandler : AuthorizationHandler<ValidateTokenRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;

        public ValidateTokenHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _clientFactory = clientFactory;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ValidateTokenRequirement requirement)
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;
            Token? token = httpContext?.Session.GetObjectFromJson<Token>("token");

            if (token == null)
            {
                context.Fail();
            }
            else
            {
                HttpClient httpClient = _clientFactory.CreateClient("NMPApi");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token?.AccessToken);
                //httpClient.DefaultRequestHeaders.Add("AuthKey", ConfigHelper.FedxAuthKey);
                var response = httpClient.GetAsync("APIURLHelper.AuthorizeBCIToken").Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    context.Fail();
                }
                else
                {
                    context.Succeed(requirement);
                    return Task.FromResult(0);
                }
            }

            if (context.HasFailed)
            {
                httpContext?.Session.Remove("token");
                httpContext?.Session.Clear();
            }

            return Task.CompletedTask;
        }
    }
}
