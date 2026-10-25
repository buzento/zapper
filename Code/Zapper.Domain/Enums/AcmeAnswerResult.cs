namespace Zapper.Domain.Enums;

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
