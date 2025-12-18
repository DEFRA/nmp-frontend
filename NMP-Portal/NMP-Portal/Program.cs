using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using GovUk.Frontend.AspNetCore;
using Joonasw.AspNetCore.SecurityHeaders;
using Joonasw.AspNetCore.SecurityHeaders.Csp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI;
using NMP.Commons.Models;
using NMP.Portal.Models;
using NMP.Portal.Security;
using NMP.Portal.Services;
using NMP.Registrar;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StackExchange.Redis; // Add this at the top of the file
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(c => c.AddServerHeader = false);
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

string? azureRedisHost = builder.Configuration["AZURE_REDIS_HOST"]?.ToString();
if (!string.IsNullOrWhiteSpace(azureRedisHost))
{
    // 1. Create the Redis multiplexer ONCE
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var credential = new DefaultAzureCredential();

        // AAD token provider for Redis Enterprise
        var options = ConfigurationOptions.Parse(azureRedisHost);
        options.Protocol = RedisProtocol.Resp3;
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 5;           // Retry 5 times
        options.ConnectTimeout = 15000;     // 15 seconds
        options.ReconnectRetryPolicy = new ExponentialRetry(5000); // Backoff strategy
        options.ConfigureForAzureWithTokenCredentialAsync(credential); // ⭐ Critical: auto-refresh AAD token

        return ConnectionMultiplexer.Connect(options);
    });

    // 2. Use Redis cache using DI-bound multiplexer
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.ConnectionMultiplexerFactory = async () =>
        {
            return builder.Services
                .BuildServiceProvider()
                .GetRequiredService<IConnectionMultiplexer>();
        };

        options.InstanceName = "nmp_ui_";
    });
}

var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.ToString();
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(t => t.AddAspNetCoreInstrumentation())
        .WithMetrics(m => m.AddAspNetCoreInstrumentation())
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = applicationInsightsConnectionString;
        });

    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
    });
}

builder.Services.AddHttpsRedirection(options => { });
builder.Services.AddHttpContextAccessor();

builder.Services.AddDefraCustomerIdentity(builder);
Registrar.RegisterDependencies(builder.Services, builder.Configuration);

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
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "NMP-Portal.Session";
    options.Cookie.HttpOnly = true;  // Prevent JavaScript access
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Only send over HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;// Prevent CSRF
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(60);  // Session timeout
    options.IOTimeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.AddApplicationInsights();
    builder.AddOpenTelemetry();
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

builder.Services.AddScoped<FarmContext>();
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
builder.Services.AddSingleton<IUserExtensionService, UserExtensionService>();
builder.Services.AddSingleton<ISnsAnalysisService, SnsAnalysisService>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IStorageCapacityService, StorageCapacityService>();
builder.Services.AddSingleton<IPreviousCroppingService, PreviousCroppingService>();
builder.Services.AddSingleton<IWarningService, WarningService>();
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

builder.Services.AddMvc(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.MaxModelBindingCollectionSize = int.MaxValue;
});

builder.Services.AddGovUkFrontend(options =>
{
    options.Rebrand = true;
    // Un-comment this block if you want to use a CSP nonce instead of hashes
    options.GetCspNonceForRequest = context =>
    {
        var cspService = context.RequestServices.GetRequiredService<ICspNonceService>();
        return cspService.GetNonce();
    };
});
builder.Services.AddCsp(nonceByteAmount: 32);

var app = builder.Build();
app.UseGovUkFrontend();
app.UseMiddleware<SecurityHeadersMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Configure the HTTP request pipeline.    
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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/favicon.ico")
    {
        context.Response.Redirect("/assets/rebrand/images/favicon.ico");
        return;
    }
    else if(context.Request.Path == "/favicon.svg")
    {
        context.Response.Redirect("/assets/rebrand/images/favicon.svg");
        return;
    }
    await next();
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
    csp.AllowImages.FromSelf().From("data:").From("https:");
    csp.AllowWorkers.FromSelf().From("blob:");
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FarmContextMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();



