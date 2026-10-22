using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Zapper.ApiCommon.OpenApi;

public sealed class ApiInformationDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly OpenApiOptions options;

    public ApiInformationDocumentTransformer(IOptions<OpenApiOptions> options) =>
        this.options = options.Value;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = options.Title;
        document.Info.Description = options.Description;
        document.Info.Version = context.DocumentName;

        return Task.CompletedTask;
    }
}
