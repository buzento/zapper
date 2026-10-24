namespace Zapper.Domain.Payment;

public enum NotificationSource
{
    PaymentAdvice = 1,
    TransactionStatus = 2
}

public sealed class NotificationSourceTypeEntity
{
    public NotificationSource Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
