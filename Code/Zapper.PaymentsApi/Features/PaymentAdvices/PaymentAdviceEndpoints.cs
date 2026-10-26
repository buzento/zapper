namespace Zapper.PaymentsApi.Features.PaymentAdvices;

public static class PaymentAdviceEndpoints
{
    public static void MapPaymentAdvices(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/payment-advices").WithTags("payment-advices");

        RecordPaymentAdvice.Map(group);
    }
}
