using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Zapper.ApiCommon.Health;

public sealed class GetHealth
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle)
             .WithSummary("Reports that the API is running.")
             .Produces(200)
             .AllowAnonymous();

    private static Ok Handle() => TypedResults.Ok();
}
