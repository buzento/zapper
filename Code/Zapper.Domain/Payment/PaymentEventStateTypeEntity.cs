namespace Zapper.Domain.Payment;

public enum PaymentEventState
{
    Held = 1,
    Applied = 2,
    NotApplied = 3,
    Contradiction = 4
}

public sealed class PaymentEventStateTypeEntity
{
    public PaymentEventState Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
