using Zapper.Domain.Enums;

namespace Zapper.Domain.Entities.Payment;

public sealed class PaymentEventEntity
{
    // only PaymentEntity makes an event, from a Transaction Status that names a reversal or a refund
    internal PaymentEventEntity(
        Guid paymentId,
        Guid notificationId,
        PaymentEvent paymentEvent,
        long amount,
        string currencyCode,
        DateTimeOffset acmeProcessedAt)
    {
        Id = Guid.CreateVersion7();
        PaymentId = paymentId;
        NotificationId = notificationId;
        Event = paymentEvent;
        Amount = amount;
        CurrencyCode = currencyCode;
        AcmeProcessedAt = acmeProcessedAt;
        State = PaymentEventState.Held;
    }

    // EF Core materialises a stored event through this one, since the constructor above names the event's type
    // apart from its property
    private PaymentEventEntity()
    {
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public Guid NotificationId { get; private set; }

    public PaymentEvent Event { get; private set; }

    public long Amount { get; private set; }

    // as Acme Switch sent it
    public string CurrencyCode { get; private set; } = string.Empty;

    public DateTimeOffset AcmeProcessedAt { get; private set; }

    public PaymentEventState State { get; private set; }

    public DateTimeOffset? AppliedAt { get; private set; }

    internal void Apply(DateTimeOffset now)
    {
        State = PaymentEventState.Applied;
        AppliedAt = now;
    }

    internal void LeaveNotApplied() => State = PaymentEventState.NotApplied;

    internal void MarkContradiction() => State = PaymentEventState.Contradiction;
}
