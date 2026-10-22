using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Zapper.ApiCommon.OpenApi;

public sealed class OperationSecurityTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security = [];

            return Task.CompletedTask;
        }

        throw new NotSupportedException(
            "An operation requiring authorization has no scheme for this transformer to name yet. "
            + "The authorization feature's planning fixes the schemes and how a permission is represented, "
            + "and this transformer is revisited once that exists.");
    }
}
