namespace Zapper.Domain.Payment;

public enum PaymentChangeSource
{
    Send = 1,
    Query = 2,
    Worker = 3
}

public sealed class PaymentChangeSourceTypeEntity
{
    public PaymentChangeSource Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
