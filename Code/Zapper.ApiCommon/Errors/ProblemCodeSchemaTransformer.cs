using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Zapper.ApiCommon.Errors;

public sealed class ProblemCodeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type != typeof(ProblemDetails)
            && context.JsonTypeInfo.Type != typeof(HttpValidationProblemDetails))
        {
            return Task.CompletedTask;
        }

        var properties = schema.Properties ?? new Dictionary<string, IOpenApiSchema>();
        properties["code"] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties = properties;

        var required = schema.Required ?? new HashSet<string>();
        required.Add("code");
        schema.Required = required;

        return Task.CompletedTask;
    }
}
