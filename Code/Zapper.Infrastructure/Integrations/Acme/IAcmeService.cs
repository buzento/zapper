namespace Zapper.Infrastructure.Integrations.Acme;

public interface IAcmeService
{
    Task<SubmitPaymentResponse> SubmitPaymentAsync(
        SubmitPaymentRequest request,
        CancellationToken cancellationToken);

    Task<GetTransactionByTransactionIdResponse> GetTransactionByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken);
}
