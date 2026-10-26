namespace Zapper.Domain.Enums;

public enum AcmeAnswerType
{
    Success,
    Failure,
    Processing,
    NotFound,
    RateLimited,
    Refused,
    OrderNumberHeldByAnother,
    OrderNumberHeldByThis,
    Degraded,
    TimedOut,
    ConnectionFailed,
    Unreadable,
    CircuitOpen
}
