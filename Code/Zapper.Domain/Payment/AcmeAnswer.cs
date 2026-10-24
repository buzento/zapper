namespace Zapper.Domain.Payment;

// what Acme said, as AcmeService read it, so no Infrastructure type reaches the Domain
public sealed class AcmeAnswer
{
    public AcmeAnswer(
        AcmeAnswerType type,
        string? responseSource = null,
        string? responseCode = null,
        string? responseDescription = null,
        string? clientMessage = null,
        string? authorisationCode = null,
        long? authorisedAmount = null,
        string? reconciliationId = null,
        string? retrievalReferenceNumber = null,
        DateTimeOffset? acmeTransactionAt = null,
        TimeSpan? retryAfter = null)
    {
        Type = type;
        ResponseSource = responseSource;
        ResponseCode = responseCode;
        ResponseDescription = responseDescription;
        ClientMessage = clientMessage;
        AuthorisationCode = authorisationCode;
        AuthorisedAmount = authorisedAmount;
        ReconciliationId = reconciliationId;
        RetrievalReferenceNumber = retrievalReferenceNumber;
        AcmeTransactionAt = acmeTransactionAt;
        RetryAfter = retryAfter;
    }

    public AcmeAnswerType Type { get; }

    public string? ResponseSource { get; }

    public string? ResponseCode { get; }

    public string? ResponseDescription { get; }

    public string? ClientMessage { get; }

    public string? AuthorisationCode { get; }

    public long? AuthorisedAmount { get; }

    public string? ReconciliationId { get; }

    public string? RetrievalReferenceNumber { get; }

    public DateTimeOffset? AcmeTransactionAt { get; }

    public TimeSpan? RetryAfter { get; }
}

public enum AcmeAnswerType
{
    Success,
    Failure,
    Processing,
    NotFound,
    RateLimited,
    Refused,
    OrderNumberHeldByAnother,
    OrderNumberHeldByThis,
    Degraded,
    TimedOut,
    ConnectionFailed,
    Unreadable,
    CircuitOpen
}
