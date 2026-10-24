namespace Zapper.PaymentsApi.Features.Payments;

public static class PaymentEndpoints
{
    public static void MapPayments(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/payments").WithTags("payments");

        GetPayment.Map(group);
        RecordPayment.Map(group);
    }
}
