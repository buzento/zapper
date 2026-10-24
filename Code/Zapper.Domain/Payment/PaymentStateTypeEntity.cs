namespace Zapper.Domain.Payment;

public enum PaymentState
{
    Recorded = 1,
    OutcomeUnknown = 2,
    Approved = 3,
    Declined = 4,
    Failed = 5,
    Unresolved = 6
}

public sealed class PaymentStateTypeEntity
{
    public PaymentState Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
