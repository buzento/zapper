namespace Zapper.Domain.Payment;

public sealed class PaymentChangeEntity
{
    // only PaymentEntity makes a change, as its state changes or a final answer confirms it
    internal PaymentChangeEntity(
        Guid paymentId,
        PaymentState fromState,
        PaymentState toState,
        PaymentChangeSource source,
        Guid? acmeInteractionId,
        DateTimeOffset changedAt)
    {
        Id = Guid.CreateVersion7();
        PaymentId = paymentId;
        FromState = fromState;
        ToState = toState;
        Source = source;
        AcmeInteractionId = acmeInteractionId;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public PaymentState FromState { get; private set; }

    public PaymentState ToState { get; private set; }

    public PaymentChangeSource Source { get; private set; }

    public Guid? AcmeInteractionId { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }
}
