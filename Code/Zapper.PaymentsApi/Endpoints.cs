using Zapper.ApiCommon.Health;
using Zapper.ApiCommon.OpenApi;
using Zapper.ApiCommon.Versioning;

namespace Zapper.PaymentsApi;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapVersion(1);

        v1.MapHealth();

        app.MapDocumentation();
    }
}
