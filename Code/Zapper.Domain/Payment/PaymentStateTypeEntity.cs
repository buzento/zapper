namespace Zapper.Domain.Payment;

public enum PaymentState
{
    Recorded = 1
}

public sealed class PaymentStateTypeEntity
{
    public PaymentState Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
