using Zapper.ApiCommon.Health;
using Zapper.ApiCommon.OpenApi;
using Zapper.ApiCommon.Versioning;
using Zapper.PaymentsApi.Features.PaymentAdvices;
using Zapper.PaymentsApi.Features.Payments;
using Zapper.PaymentsApi.Features.TransactionStatuses;

namespace Zapper.PaymentsApi;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var v1 = app.MapVersion(1);

        v1.MapHealth();
        v1.MapPayments();
        v1.MapPaymentAdvices();
        v1.MapTransactionStatuses();

        app.MapDocumentation();
    }
}
