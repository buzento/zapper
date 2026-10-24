namespace Zapper.Domain.Payment;

public enum AcmeAnswerResult
{
    Approved,
    Declined,
    Failed,
    FailedRefusedByAcme,
    FailedOrderNumberInUse,
    FailedNotSubmitted,
    FailedAfterUnexpectedFailures,
    SendAgain,
    QueryLater,
    Postponed,
    Unresolved
}
