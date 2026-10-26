using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.PaymentsApi;

public sealed class PaymentsApiV1ContractTests : IClassFixture<PaymentsApiFixture>
{
    private const string ComponentSchemaPrefix = "#/components/schemas/";

    private readonly IServiceProvider provider;

    public PaymentsApiV1ContractTests(PaymentsApiFixture api) => provider = api.Services;

    public static TheoryData<string, string> FrozenFragments
    {
        get
        {
            var contracts = GetContractsFolder();
            var cases = new TheoryData<string, string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(contracts, "v1"), "*.openapi.json", SearchOption.AllDirectories))
            {
                var route = Path.GetRelativePath(contracts, Path.GetDirectoryName(path) ?? contracts).Replace('\\', '/');

                cases.Add($"/{route}", Path.GetFileName(path).Replace(".openapi.json", string.Empty, StringComparison.Ordinal));
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(FrozenFragments))]
    public async Task GeneratedDocument_FrozenFragment_MatchesExactly(string route, string method)
    {
        var documentProvider = provider.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var fragmentText = await File.ReadAllTextAsync(
            Path.Combine(GetContractsFolder(), route.TrimStart('/'), $"{method}.openapi.json"),
            TestContext.Current.CancellationToken);
        var fragment = JsonNode.Parse(fragmentText);
        var fragmentOperation = fragment?["paths"]?[route]?[method];
        var fragmentComponents = GetReferencedComponents(fragment, fragmentOperation);

        var document = await documentProvider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
        var generated = JsonNode.Parse(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1, TestContext.Current.CancellationToken));
        var generatedOperation = generated?["paths"]?[route]?[method];
        var generatedComponents = GetReferencedComponents(generated, generatedOperation);

        generatedOperation.ShouldNotBeNull($"The generated document holds no {method} on {route}.");
        JsonNode.DeepEquals(generatedOperation, fragmentOperation)
            .ShouldBeTrue($"The operation differs.\nGenerated: {generatedOperation}\nFrozen: {fragmentOperation}");
        JsonNode.DeepEquals(generatedComponents, fragmentComponents)
            .ShouldBeTrue($"A schema or a security scheme differs.\nGenerated: {generatedComponents}\nFrozen: {fragmentComponents}");
        OpenApiDocument.Parse(fragmentText, "json").Diagnostic.ShouldNotBeNull().Errors.ShouldBeEmpty();
    }

    private static JsonObject GetReferencedComponents(JsonNode? document, JsonNode? operation)
    {
        var schemas = new JsonObject();
        var pending = new Queue<JsonNode?>([operation]);

        while (pending.TryDequeue(out var node))
        {
            foreach (var name in GetSchemaReferences(node).Where(name => !schemas.ContainsKey(name)))
            {
                var schema = document?["components"]?["schemas"]?[name]?.DeepClone();

                schemas[name] = schema;
                pending.Enqueue(schema);
            }
        }

        var securitySchemes = new JsonObject();

        foreach (var name in operation?["security"]?.AsArray().SelectMany(GetSchemeNames) ?? [])
        {
            securitySchemes[name] = document?["components"]?["securitySchemes"]?[name]?.DeepClone();
        }

        return new JsonObject { ["schemas"] = schemas, ["securitySchemes"] = securitySchemes };
    }

    private static IEnumerable<string> GetSchemaReferences(JsonNode? node) => node switch
    {
        JsonObject members => members.SelectMany(member =>
            member is { Key: "$ref", Value: JsonValue reference }
            && reference.GetValue<string>() is { } target
            && target.StartsWith(ComponentSchemaPrefix, StringComparison.Ordinal)
                ? [target[ComponentSchemaPrefix.Length..]]
                : GetSchemaReferences(member.Value)),
        JsonArray items => items.SelectMany(GetSchemaReferences),
        _ => [],
    };

    private static IEnumerable<string> GetSchemeNames(JsonNode? requirement) =>
        requirement is JsonObject schemes ? schemes.Select(scheme => scheme.Key) : [];

    private static string GetContractsFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !folder.EnumerateFiles("*.slnx").Any())
        {
            folder = folder.Parent;
        }

        // the test assembly is built inside Code/, which holds the solution file
        return Path.Combine(folder!.Parent!.FullName, "Docs", "Contracts", "PaymentsApi");
    }
}
