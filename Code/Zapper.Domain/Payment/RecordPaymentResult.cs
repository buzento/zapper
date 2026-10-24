namespace Zapper.Domain.Payment;

public enum RecordPaymentResult
{
    Allowed,
    AlreadyRecorded,
    AppPaymentReferenceReused,
    AmountAboveMaximum
}
