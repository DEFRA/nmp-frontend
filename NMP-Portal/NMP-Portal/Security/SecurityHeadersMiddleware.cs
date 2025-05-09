﻿using Microsoft.Extensions.Primitives;
using System.Reflection.PortableExecutable;

namespace NMP.Portal.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate requestDelegate)
{
    private readonly RequestDelegate _requestDelegate = requestDelegate;

    public Task Invoke(HttpContext context)
    {
        // Remove technology disclosure headers
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("Server");
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
        // TODO Change the value depending of your needs
        context.Response.Headers.Append("Referrer-Policy", new StringValues("strict-origin"));

        // Strict Transport Security (HSTS) - Enforces HTTPS
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
        context.Response.Headers.Append("X-Content-Type-Options", new StringValues("nosniff"));

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
        context.Response.Headers.Append("X-Frame-Options", new StringValues("DENY"));

        // https://security.stackexchange.com/questions/166024/does-the-x-permitted-cross-domain-policies-header-have-any-benefit-for-my-websit
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", new StringValues("none"));

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-XSS-Protection
        //context.Response.Headers.Append("X-Xss-Protection", new StringValues("1; mode=block"));

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Expect-CT
        // You can use https://report-uri.com/ to get notified when a misissued certificate is detected
        //context.Response.Headers.Add("Expect-CT", new StringValues("max-age=0, enforce, report-uri=\"https://example.report-uri.com/r/d/ct/enforce\""));

        context.Response.Headers.Append("Permissions-Policy", new StringValues(
            "accelerometer=(self), " +
            "autoplay=(self), " +
            "camera=(self), " +
            "display-capture=(self), " +
            // "document-domain=(self), " +
            "midi=(self), " +
            "publickey-credentials-get=(self), " +
            "sync-xhr=(self), " +
            "xr-spatial-tracking=(self), " +
            "geolocation=(self), " +
            "gyroscope=(self), " +
            "magnetometer=(self), " +
            "microphone=(self), " +
            "payment=(self), " +
            "usb=(self)"));

        // Remove X-XSS-Protection (deprecated)
        context.Response.Headers.Remove("X-XSS-Protection");

        // https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
        // TODO change the value of each rule and check the documentation to see if new rules are available
        //context.Response.Headers.Add("Content-Security-Policy", new StringValues(
        //    "base-uri 'self';" +
        //    "block-all-mixed-content;" +
        //    "child-src 'self';" +
        //    "connect-src 'self' wss: ws:;" +
        //    "default-src 'self';" +
        //    "font-src 'self' https: " + //fonts.googleapis.com;
        //    "form-action 'self';" +
        //    "frame-ancestors 'self';" +
        //    "frame-src 'self';" +
        //    "img-src * 'self' data: https:;" +
        //    "manifest-src 'self';" +
        //    "media-src 'self';" +
        //    "object-src 'self';" +
        //    // "sandbox 'self' allow-scripts;" +
        //    "script-src 'self' 'unsafe-inline'  " + //ajax.aspnetcdn.com www.gstatic.com;  ajax.aspnetcdn.com stackpath.bootstrapcdn.com www.gstatic.com cdn.jsdelivr.net;" +
        //     "script-src-attr 'self' 'unsafe-inline'  " + //ajax.aspnetcdn.com www.gstatic.com;  ajax.aspnetcdn.com stackpath.bootstrapcdn.com;" +
        //     "script-src-elem 'self' 'unsafe-inline'  " + //ajax.aspnetcdn.com www.gstatic.com;  ajax.aspnetcdn.com stackpath.bootstrapcdn.com;" +
        //    "style-src 'self' 'unsafe-inline'  " +  //www.gstatic.com; stackpath.bootstrapcdn.com www.gstatic.com cdn.jsdelivr.net;" +
        //     "style-src-attr 'self' 'unsafe-inline' " + // www.gstatic.com; stackpath.bootstrapcdn.com;" +
        //     "style-src-elem 'self' 'unsafe-inline'  " + //fonts.googleapis.com www.gstatic.com; stackpath.bootstrapcdn.com;" +
        //    "upgrade-insecure-requests;" +
        //    "worker-src 'self';"
        //    ));
                

        return _requestDelegate(context);
    }


}
