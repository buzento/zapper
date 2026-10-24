namespace Zapper.Domain.Payment;

// a notification is stored once, as it was received, and never rewritten; what was done with it is decided before
// it is first saved
public sealed class NotificationEntity
{
    public NotificationEntity(
        NotificationSource source,
        string acmeReference,
        string? requestType,
        byte[] contentHash,
        string body,
        string? transactionStatus,
        string? responseCode,
        string traceId,
        TimeProvider time)
    {
        Id = Guid.CreateVersion7();
        Source = source;
        AcmeReference = acmeReference;
        RequestType = requestType;
        ContentHash = contentHash;
        Body = body;
        TransactionStatus = transactionStatus;
        ResponseCode = responseCode;
        TraceId = traceId;
        ReceivedAt = time.GetUtcNow();
        Outcome = NotificationOutcome.Recorded;
    }

    // EF Core materialises a stored notification through this one, since the constructor above takes the clock
    private NotificationEntity()
    {
    }

    public Guid Id { get; private set; }

    public NotificationSource Source { get; private set; }

    public string AcmeReference { get; private set; } = string.Empty;

    public string? RequestType { get; private set; }

    public byte[] ContentHash { get; private set; } = [];

    public Guid? PaymentId { get; private set; }

    public NotificationOutcome Outcome { get; private set; }

    public string? TransactionStatus { get; private set; }

    public string? ResponseCode { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; private set; }

    public string TraceId { get; private set; } = string.Empty;

    // a notification that matches no payment is never matched later, R-6
    public void RecordUnmatched() => Outcome = NotificationOutcome.Unmatched;

    internal void RecordForPayment(Guid paymentId, NotificationOutcome outcome)
    {
        PaymentId = paymentId;
        Outcome = outcome;
    }
}
