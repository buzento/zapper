namespace Zapper.Domain.Payment;

public enum NotificationOutcome
{
    Applied = 1,
    Confirmed = 2,
    Held = 3,
    Recorded = 4,
    Unmatched = 5,
    Contradiction = 6
}

public sealed class NotificationOutcomeTypeEntity
{
    public NotificationOutcome Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
