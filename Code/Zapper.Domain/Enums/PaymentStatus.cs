namespace Zapper.Domain.Enums;

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
