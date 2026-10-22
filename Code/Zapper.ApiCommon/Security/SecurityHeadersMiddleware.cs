using Microsoft.AspNetCore.Http;

namespace Zapper.ApiCommon.Security;

public static class SecurityHeadersMiddleware
{
    private const string DocumentationPathPrefix = "/scalar";

    public static async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            context.Response.Headers.XContentTypeOptions = "nosniff";

            if (!context.Request.Path.StartsWithSegments(DocumentationPathPrefix))
            {
                context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'";
            }

            return Task.CompletedTask;
        });

        await next();
    }
}
