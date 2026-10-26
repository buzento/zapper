namespace Zapper.Domain.Enums;

public enum PaymentState
{
    Recorded = 1,
    OutcomeUnknown = 2,
    Approved = 3,
    Declined = 4,
    Failed = 5,
    Unresolved = 6,
    Reversed = 7,
    Refunded = 8,
    PartiallyRefunded = 9
}
