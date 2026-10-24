namespace Zapper.Domain.Payment;

public enum AcmeInteractionOutcome
{
    Answered = 1,
    TimedOut = 2,
    ConnectionFailed = 3
}

public sealed class AcmeInteractionOutcomeTypeEntity
{
    public AcmeInteractionOutcome Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
