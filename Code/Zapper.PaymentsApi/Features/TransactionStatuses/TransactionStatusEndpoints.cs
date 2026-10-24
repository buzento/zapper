namespace Zapper.PaymentsApi.Features.TransactionStatuses;

public static class TransactionStatusEndpoints
{
    public static void MapTransactionStatuses(this IEndpointRouteBuilder version)
    {
        var group = version.MapGroup("/transaction-statuses").WithTags("transaction-statuses");

        RecordTransactionStatus.Map(group);
    }
}
