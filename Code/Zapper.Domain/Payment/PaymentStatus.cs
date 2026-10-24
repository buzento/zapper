namespace Zapper.Domain.Payment;

public enum PaymentStatus
{
    Pending,
    Approved,
    Declined,
    Failed,
    Reversed,
    Refunded,
    PartiallyRefunded,
    Unresolved
}
