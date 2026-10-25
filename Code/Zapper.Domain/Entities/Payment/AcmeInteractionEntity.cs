using Zapper.Domain.Enums;

namespace Zapper.Domain.Entities.Payment;

public sealed class AcmeInteractionEntity
{
    public AcmeInteractionEntity(
        Guid paymentId,
        Guid storedCardId,
        AcmeOperation operation,
        AcmeInteractionOutcome outcome,
        Guid? messageId,
        int attempt,
        string request,
        int? httpStatus,
        string? response,
        DateTimeOffset startedAt,
        int durationMilliseconds,
        string traceId)
    {
        Id = Guid.CreateVersion7();
        PaymentId = paymentId;
        StoredCardId = storedCardId;
        Operation = operation;
        Outcome = outcome;
        MessageId = messageId;
        Attempt = attempt;
        Request = request;
        HttpStatus = httpStatus;
        Response = response;
        StartedAt = startedAt;
        DurationMilliseconds = durationMilliseconds;
        TraceId = traceId;
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public Guid StoredCardId { get; private set; }

    public AcmeOperation Operation { get; private set; }

    public AcmeInteractionOutcome Outcome { get; private set; }

    public Guid? MessageId { get; private set; }

    public int Attempt { get; private set; }

    public string Request { get; private set; }

    public int? HttpStatus { get; private set; }

    public string? Response { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public int DurationMilliseconds { get; private set; }

    public string TraceId { get; private set; }
}
