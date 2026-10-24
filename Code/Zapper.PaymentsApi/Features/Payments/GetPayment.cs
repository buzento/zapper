using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Zapper.Infrastructure.Persistence;

namespace Zapper.PaymentsApi.Features.Payments;

public sealed class GetPayment
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
             .WithSummary("Returns a payment's current status.")
             .Produces<GetPaymentResponse>(200)
             .ProducesProblem(404)
             .AllowAnonymous();

    private static async Task<Results<Ok<GetPaymentResponse>, NotFound>> HandleAsync(
        Guid id,
        ZapperDbContext database,
        CancellationToken cancellationToken)
    {
        var payment = await database.Payments
            .Where(candidate => candidate.Id == id)
            .Join(
                database.CurrencyTypes,
                candidate => candidate.Currency,
                currency => currency.Id,
                (candidate, currency) => new GetPaymentResponse(
                    candidate.Id,
                    candidate.AppPaymentReference,
                    candidate.GetStatus().ToString(),
                    candidate.Amount,
                    currency.Code,
                    candidate.AuthorisedAmount,
                    candidate.ResponseCode,
                    candidate.ClientMessage,
                    candidate.ReversedAmount,
                    candidate.RefundedAmount))
            .SingleOrDefaultAsync(cancellationToken);

        return payment is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(payment);
    }
}

public sealed record GetPaymentResponse(
    Guid Id,
    string AppPaymentReference,
    string Status,
    long Amount,
    string CurrencyCode,
    long? AuthorisedAmount,
    string? ResponseCode,
    string? ClientMessage,
    long ReversedAmount,
    long RefundedAmount);
