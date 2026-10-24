using Zapper.ApiCommon.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Zapper.ApiCommon.OpenApi;

// an operation's permission is its policy's name, and the policy names the one scheme that grants it, so the
// requirement is read from the operation's authorization metadata and never stated by hand, BackendCode.md section 19
public sealed class OperationSecurityTransformer : IOpenApiOperationTransformer
{
    private readonly IAuthorizationPolicyProvider policyProvider;

    private readonly IAuthenticationSchemeProvider schemeProvider;

    private readonly IOptionsMonitor<BodySignatureAuthenticationSchemeOptions> bodySignatureOptions;

    public OperationSecurityTransformer(
        IAuthorizationPolicyProvider policyProvider,
        IAuthenticationSchemeProvider schemeProvider,
        IOptionsMonitor<BodySignatureAuthenticationSchemeOptions> bodySignatureOptions)
    {
        this.policyProvider = policyProvider;
        this.schemeProvider = schemeProvider;
        this.bodySignatureOptions = bodySignatureOptions;
    }

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];

            return;
        }

        var permissions = metadata.OfType<IAuthorizeData>().Select(data => data.Policy).OfType<string>().ToList();

        if (permissions.Count == 0 || context.Document is null)
        {
            throw new NotSupportedException(
                "An operation requiring authorization names no permission for this transformer to write.");
        }

        var requirement = new OpenApiSecurityRequirement();

        foreach (var permission in permissions)
        {
            var policy = await policyProvider.GetPolicyAsync(permission)
                ?? throw new NotSupportedException($"The permission {permission} has no policy.");

            foreach (var schemeName in policy.AuthenticationSchemes)
            {
                var scheme = await schemeProvider.GetSchemeAsync(schemeName)
                    ?? throw new NotSupportedException($"The scheme {schemeName} is not registered.");

                context.Document.AddComponent<IOpenApiSecurityScheme>(schemeName, GetSecurityScheme(scheme));
                requirement[new OpenApiSecuritySchemeReference(schemeName, context.Document)] = [permission];
            }
        }

        operation.Security = [requirement];
    }

    private OpenApiSecurityScheme GetSecurityScheme(AuthenticationScheme scheme)
    {
        if (scheme.HandlerType == typeof(BasicAuthenticationHandler))
        {
            return new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "basic" };
        }

        if (scheme.HandlerType == typeof(BodySignatureAuthenticationHandler))
        {
            return new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = bodySignatureOptions.Get(scheme.Name).HeaderName,
                In = ParameterLocation.Header,
            };
        }

        throw new NotSupportedException($"The scheme {scheme.Name} has a handler this transformer does not describe.");
    }
}
