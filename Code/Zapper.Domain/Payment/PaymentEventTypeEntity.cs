namespace Zapper.Domain.Payment;

public enum PaymentEvent
{
    Reversal = 1,
    Refund = 2
}

public sealed class PaymentEventTypeEntity
{
    public PaymentEvent Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
