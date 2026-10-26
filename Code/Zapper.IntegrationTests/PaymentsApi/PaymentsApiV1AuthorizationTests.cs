using System.Net;
using System.Text.Json.Nodes;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.PaymentsApi;

// every operation of this version is public or called by a provider's system, so the class holds the 401 sweep
// alone, Testing.md section 8
public sealed class PaymentsApiV1AuthorizationTests : IClassFixture<PaymentsApiFixture>
{
    private readonly HttpClient client;

    public PaymentsApiV1AuthorizationTests(PaymentsApiFixture api) => client = api.CreateClient();

    public static TheoryData<string, string> OperationsRequiringSignIn
    {
        get
        {
            var contracts = GetContractsFolder();
            var cases = new TheoryData<string, string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(contracts, "v1"), "*.openapi.json", SearchOption.AllDirectories))
            {
                var route = $"/{Path.GetRelativePath(contracts, Path.GetDirectoryName(path) ?? contracts).Replace('\\', '/')}";
                var method = Path.GetFileName(path).Replace(".openapi.json", string.Empty, StringComparison.Ordinal);
                var security = JsonNode.Parse(File.ReadAllText(path))?["paths"]?[route]?[method]?["security"]?.AsArray();

                if (security is { Count: > 0 })
                {
                    cases.Add(route, method);
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(OperationsRequiringSignIn))]
    public async Task Operation_SignedOut_ReturnsUnauthorized(string route, string method)
    {
        // each key in the route is filled with its own id, and no body is sent
        var filledRoute = string.Join(
            '/',
            route.Split('/').Select(segment => segment.StartsWith('{') ? Guid.CreateVersion7().ToString() : segment));
        using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), filledRoute);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

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
