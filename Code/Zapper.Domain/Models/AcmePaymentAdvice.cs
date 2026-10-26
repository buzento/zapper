using System.Globalization;

namespace Zapper.Domain.Models;

// what a Payment Advice said, as the operation read it, so no request type reaches the Domain
public sealed class AcmePaymentAdvice
{
    public AcmePaymentAdvice(
        string requestType,
        string merchantId,
        string transactionId,
        DateTimeOffset? acmeTransactionAt,
        string? orderNumber,
        string? reconciliationId,
        string retrievalReferenceNumber,
        string systemsTraceAuditNumber,
        long amount,
        string? currencyCode,
        string paymentService,
        string transactionStatus,
        string responseSource,
        string responseCode,
        string? responseDescription,
        string? authorisationCode,
        string communicationProtocol)
    {
        RequestType = requestType;
        MerchantId = merchantId;
        TransactionId = transactionId;
        AcmeTransactionAt = acmeTransactionAt;
        OrderNumber = orderNumber;
        ReconciliationId = reconciliationId;
        RetrievalReferenceNumber = retrievalReferenceNumber;
        SystemsTraceAuditNumber = systemsTraceAuditNumber;
        Amount = amount;
        CurrencyCode = currencyCode;
        PaymentService = paymentService;
        TransactionStatus = transactionStatus;
        ResponseSource = responseSource;
        ResponseCode = responseCode;
        ResponseDescription = responseDescription;
        AuthorisationCode = authorisationCode;
        CommunicationProtocol = communicationProtocol;
    }

    public string RequestType { get; }

    public string MerchantId { get; }

    public string TransactionId { get; }

    public DateTimeOffset? AcmeTransactionAt { get; }

    public string? OrderNumber { get; }

    public string? ReconciliationId { get; }

    public string RetrievalReferenceNumber { get; }

    public string SystemsTraceAuditNumber { get; }

    public long Amount { get; }

    public string? CurrencyCode { get; }

    public string PaymentService { get; }

    public string TransactionStatus { get; }

    public string ResponseSource { get; }

    public string ResponseCode { get; }

    public string? ResponseDescription { get; }

    public string? AuthorisationCode { get; }

    public string CommunicationProtocol { get; }

    // the text the content hash is made from: only the mapped fields, so a member Acme adds, or one that changes
    // between deliveries, never makes a delivery different, R-4
    public string GetContent() =>
        string.Join(
            '\n',
            $"RequestType={RequestType}",
            $"MerchantID={MerchantId}",
            $"TransactionID={TransactionId}",
            $"TransactionDateTime={AcmeTransactionAt?.ToString("O", CultureInfo.InvariantCulture)}",
            $"OrderNumber={OrderNumber}",
            $"ReconID={ReconciliationId}",
            $"RetrievalReferenceNumber={RetrievalReferenceNumber}",
            $"SystemsTraceAuditNumber={SystemsTraceAuditNumber}",
            $"Amount={Amount.ToString(CultureInfo.InvariantCulture)}",
            $"CurrencyCode={CurrencyCode}",
            $"PaymentService={PaymentService}",
            $"TransactionStatus={TransactionStatus}",
            $"ResponseSource={ResponseSource}",
            $"ResponseCode={ResponseCode}",
            $"ResponseDescription={ResponseDescription}",
            $"AuthCode={AuthorisationCode}",
            $"CommProtocol={CommunicationProtocol}");
}
