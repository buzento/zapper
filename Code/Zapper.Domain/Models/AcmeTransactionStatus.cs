using System.Globalization;

namespace Zapper.Domain.Models;

// what a Transaction Status said, as the operation read it, so no request type reaches the Domain
public sealed class AcmeTransactionStatus
{
    public AcmeTransactionStatus(
        string transactionUuid,
        string transactionType,
        DateTimeOffset acmeProcessedAt,
        string retrievalReferenceNumber,
        long amount,
        string currencyCode,
        string transactionResultCode,
        bool isApproved)
    {
        TransactionUuid = transactionUuid;
        TransactionType = transactionType;
        AcmeProcessedAt = acmeProcessedAt;
        RetrievalReferenceNumber = retrievalReferenceNumber;
        Amount = amount;
        CurrencyCode = currencyCode;
        TransactionResultCode = transactionResultCode;
        IsApproved = isApproved;
    }

    public string TransactionUuid { get; }

    public string TransactionType { get; }

    public DateTimeOffset AcmeProcessedAt { get; }

    public string RetrievalReferenceNumber { get; }

    public long Amount { get; }

    public string CurrencyCode { get; }

    public string TransactionResultCode { get; }

    public bool IsApproved { get; }

    // the text the content hash is made from: only the mapped fields, so a member Acme adds, or one that changes
    // between deliveries, never makes a delivery different, R-4
    public string GetContent() =>
        string.Join(
            '\n',
            $"transactionUUID={TransactionUuid}",
            $"transactionType={TransactionType}",
            $"transactionDateTime={AcmeProcessedAt.ToString("O", CultureInfo.InvariantCulture)}",
            $"rrn={RetrievalReferenceNumber}",
            $"transactionAmount={Amount.ToString(CultureInfo.InvariantCulture)}",
            $"currencyCode={CurrencyCode}",
            $"txResultCode={TransactionResultCode}",
            $"isApproved={(IsApproved ? "true" : "false")}");
}
