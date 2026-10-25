namespace Zapper.Domain.Enums;

public enum RecordPaymentResult
{
    Allowed,
    AlreadyRecorded,
    AppPaymentReferenceReused,
    AmountAboveMaximum
}
