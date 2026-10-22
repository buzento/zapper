using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Zapper.ApiCommon.Versioning;

public static class VersioningExtensions
{
    public static RouteGroupBuilder MapVersion(this WebApplication app, int version) =>
        app.MapGroup($"/v{version}");
}
