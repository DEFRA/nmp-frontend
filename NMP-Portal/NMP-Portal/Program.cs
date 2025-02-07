using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using GovUk.Frontend.AspNetCore;
using Joonasw.AspNetCore.SecurityHeaders;
using Joonasw.AspNetCore.SecurityHeaders.Csp;
using NMP.Portal.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using NMP.Portal.Authorization;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using NMP.Portal.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using NMP.Portal.Helpers;
using NMP.Portal.Models;
using Microsoft.Identity.Web.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = null;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = null; // if don't set default value is: 30 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue; // if don't set default value is: 128 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.KeyLengthLimit = int.MaxValue;
    options.MultipartHeadersCountLimit = int.MaxValue;
    options.BufferBodyLengthLimit = int.MaxValue;
    options.BufferBody = true;
});

builder.Services.AddDefraCustomerIdentity(builder);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(o => o.LoginPath = new PathString("/Account/Login"));

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();

builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                  .RequireAuthenticatedUser()
                  .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();

builder.Services.AddDataProtection();
builder.Services.AddControllersWithViews().AddSessionStateTempDataProvider();
builder.Services.AddSession(options => { options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.IdleTimeout = TimeSpan.FromMinutes(20); });

var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.ToString();

if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
});



builder.Services.AddHttpClient("NMPApi", httpClient =>
{
    httpClient.BaseAddress = new Uri(uriString: builder.Configuration.GetSection("NMPApiUrl").Value ?? "/");
    httpClient.Timeout = TimeSpan.FromMinutes(5);
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("DefraIdentityConfiguration", httpClient =>
{
    httpClient.BaseAddress = new Uri(uriString: builder.Configuration.GetSection("CustomerIdentityInstance").Value ?? "/");
    httpClient.Timeout = TimeSpan.FromMinutes(5);
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();  // Access current UserName in Repository or other Custom Components
builder.Services.AddSingleton<IAddressLookupService, AddressLookupService>();
builder.Services.AddSingleton<IUserFarmService, UserFarmService>();
builder.Services.AddSingleton<IFarmService, FarmService>();
builder.Services.AddSingleton<IFieldService, FieldService>();
builder.Services.AddSingleton<ISoilService, SoilService>();
builder.Services.AddSingleton<ICropService, CropService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IOrganicManureService, OrganicManureService>();
builder.Services.AddSingleton<IMannerService, MannerService>();
builder.Services.AddSingleton<IFertiliserManureService, FertiliserManureService>();
builder.Services.AddSingleton<ISoilAnalysisService, SoilAnalysisService>();
builder.Services.AddSingleton<IPKBalanceService, PKBalanceService>();

builder.Services.ConfigureApplicationCookie(options =>
{    
    options.Cookie.Name = "NMP-Portal";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Cookie.Path = "/";
    options.SlidingExpiration = true;    
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    if (!string.IsNullOrWhiteSpace(builder.Configuration["CustomerIdentityReturnURI"]))
    {
        options.Cookie.Domain = new Uri(builder.Configuration["CustomerIdentityReturnURI"]).Authority;
    }
});

builder.Services.AddAntiforgery(options =>
{
    // Set Cookie properties using CookieBuilder properties�.
    options.Cookie = new CookieBuilder()
    {                
        Name = "NMP-Portal",
        HttpOnly = true,
        Path = "/",
        SecurePolicy = CookieSecurePolicy.Always,
        SameSite = SameSiteMode.Strict
    };

    if (!string.IsNullOrWhiteSpace(builder.Configuration["CustomerIdentityReturnURI"]))
    {
        options.Cookie.Domain = new Uri(builder.Configuration["CustomerIdentityReturnURI"]).Authority;
    }    
    options.FormFieldName = "NMP-Portal-Antiforgery-Field";
    options.HeaderName = "X-CSRF-TOKEN-NMP";
    options.SuppressXFrameOptionsHeader = false;
});

builder.Services.AddMvc(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.MaxModelBindingCollectionSize = int.MaxValue;
});

builder.Services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs }));

builder.Services.AddGovUkFrontend(options =>
{
    // Un-comment this block if you want to use a CSP nonce instead of hashes
    options.GetCspNonceForRequest = context =>
    {
        var cspService = context.RequestServices.GetRequiredService<ICspNonceService>();
        return cspService.GetNonce();
    };
});
builder.Services.AddCsp(nonceByteAmount: 32);

var app = builder.Build();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Configure the HTTP request pipeline.
    //app.UseExceptionHandler("/Error/404");   
    //app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.Use(async (ctx, next) =>
    {
        await next();
        if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
        {
            //Re-execute the request so the user gets the error page
            string originalPath = ctx.Request.Path.Value;
            ctx.Items["originalPath"] = originalPath;
            ctx.Request.Path = "/Error/404";
            await next();
        }
    });

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{    
    // Do work that doesn't write to the Response.
    if (context.Request.Method is "OPTIONS" or "TRACE" or "HEAD")
    {
        context.Response.StatusCode = 405;
        return;
    }

    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
});

app.UseCsp(csp =>
{
    var pageTemplateHelper = app.Services.GetRequiredService<PageTemplateHelper>();
    csp.ByDefaultAllow
        .FromSelf();
    csp.AllowStyles
           .FromSelf().AddNonce(); 
    csp.AllowScripts
        .FromSelf()
        .AddNonce()
        .From(pageTemplateHelper.GetCspScriptHashes())
        .From("https://*/-vs/browserLink.js");
    csp.AllowConnections.ToSelf().To("wss:").To("ws:").To("https:").To("http:");
    csp.AllowBaseUri.FromSelf();
    csp.AllowFrames.FromSelf();
    csp.AllowAudioAndVideo.FromSelf();
    csp.AllowFonts.FromSelf();
    csp.AllowManifest.FromSelf();    
    //csp.AllowScripts
    //    .FromSelf()
    //    .AllowUnsafeInline()
    //    .From("cdnjs.cloudflare.com")
    //    .AddNonce();
    ////.From(pageTemplateHelper.GetCspScriptHashes());
    csp.AllowImages.FromSelf().From("data:").From("https:");
    csp.AllowWorkers.FromSelf().From("blob:");
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
// Add the reauthentication middleware
app.UseMiddleware<ReauthenticationMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

