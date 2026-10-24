namespace Zapper.Infrastructure.Integrations.Acme;

public sealed record SubmitPaymentRequest(
    Guid MessageId,
    Guid TransactionId,
    string OrderNumber,
    long Amount,
    string CurrencyCode,
    string PaymentService,
    string Token,
    string CardOnFileScenario,
    string SubMerchantReference,
    string MerchantCategoryCode,
    string CardAcceptorName,
    string Email);
