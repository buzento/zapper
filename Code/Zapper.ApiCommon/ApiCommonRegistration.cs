using Zapper.ApiCommon.Errors;
using Zapper.ApiCommon.Managers.ErrorMessage;
using Zapper.ApiCommon.OpenApi;
using Zapper.ApiCommon.Security;
using Zapper.ApiCommon.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon;

public static class ApiCommonRegistration
{
    public static IServiceCollection AddZapperApiCommon(
        this IServiceCollection services,
        params int[] versions)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IErrorMessageManager, ErrorMessageManager>();

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = ProblemDetailsCustomizer.Customize);

        services.AddExceptionHandler<UnhandledExceptionHandler>();

        foreach (var version in versions)
        {
            var documentName = $"v{version}";

            services.AddOpenApi(documentName, options =>
            {
                options.ShouldInclude = description =>
                    description.RelativePath is string route
                    && route.StartsWith($"{documentName}/", StringComparison.Ordinal);

                options.AddSchemaTransformer<ProblemCodeSchemaTransformer>();
                options.AddDocumentTransformer<ApiInformationDocumentTransformer>();
                options.AddOperationTransformer<OperationSecurityTransformer>();
            });
        }

        services.AddOptions<OpenApiOptions>()
                .BindConfiguration("Zapper:OpenApi")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Resolve;

        services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAssertion(AllowRequestWithNoEndpointMetadata)
                    .Build());

        services.AddCors();

        services.AddRateLimiter();

        services.AddOptions<SecurityOptions>()
                .BindConfiguration("Zapper:Security")
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<CorsOptions>, CorsConfigureOptions>();
        services.AddSingleton<IConfigureOptions<KestrelServerOptions>, KestrelServerConfigureOptions>();
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterConfigureOptions>();

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

        services.Configure<JsonOptions>(options =>
            options.SerializerOptions.AllowDuplicateProperties = false);

        return services;
    }

    public static void UseZapperApiCommon(this WebApplication app)
    {
        app.Use(SecurityHeadersMiddleware.InvokeAsync);
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthorization();
    }

    private static bool AllowRequestWithNoEndpointMetadata(AuthorizationHandlerContext context) =>
        context.Resource is not HttpContext httpContext
        || httpContext.GetEndpoint()?.Metadata.Count is null or 0;
}
