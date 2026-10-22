using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Zapper.ApiCommon.OpenApi;

public static class OpenApiExtensions
{
    private const string DocumentationPathPrefix = "/scalar";

    public static void MapDocumentation(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<OpenApiOptions>>().Value.IsEnabled
            is not true)
        {
            return;
        }

        app.UseWhen(
            context => context.Request.Path.StartsWithSegments(DocumentationPathPrefix),
            branch => branch.Use(WrapDocumentationResponseAsync));

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
    }

    private static async Task WrapDocumentationResponseAsync(HttpContext context, Func<Task> next)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; script-src 'self'; connect-src 'self'; referrer-policy no-referrer";

            var contentType = context.Response.ContentType;

            if (contentType is not null
                && !contentType.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = contentType + "; charset=utf-8";
            }

            return Task.CompletedTask;
        });

        await next();
    }
}
