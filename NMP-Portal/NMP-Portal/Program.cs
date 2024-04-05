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
    options.MultipartBodyLengthLimit = Int64.MaxValue; // if don't set default value is: 128 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.KeyLengthLimit = int.MaxValue;
    options.MultipartHeadersCountLimit = int.MaxValue;
    options.BufferBodyLengthLimit = int.MaxValue;
    options.BufferBody = true;
});



// Add services to the container.
//builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => o.LoginPath = new PathString("/Account/Login"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});



builder.Services.AddDataProtection();
builder.Services.AddControllersWithViews().AddSessionStateTempDataProvider();
builder.Services.AddSession(options => { options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; });
builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
});

builder.Services.AddHttpContextAccessor(); // Access current UserName in Repository or other Custom Components
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
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
    options.FormFieldName = "NMP-Portal-Antiforgery-Field";
    options.HeaderName = "X-CSRF-TOKEN-NMP";
    options.SuppressXFrameOptionsHeader = false;

});
builder.Services.AddSession();
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    
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
           .FromSelf(); 
    csp.AllowScripts
        .FromSelf()
        //.AddNonce()
        .From(pageTemplateHelper.GetCspScriptHashes());

    csp.AllowConnections.ToSelf().To("wss:").To("ws").To("https:").To("http:");
    csp.AllowBaseUri.FromSelf();
    csp.AllowFrames.FromSelf();
    csp.AllowAudioAndVideo.FromSelf();
    csp.AllowFonts.FromSelf();
    csp.AllowManifest.FromSelf();
    //csp.AllowFormActions.To().

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

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
