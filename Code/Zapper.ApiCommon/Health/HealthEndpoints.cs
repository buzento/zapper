using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Zapper.ApiCommon.Health;

public static class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/health").WithTags("health");

        GetHealth.Map(group);
    }
}
