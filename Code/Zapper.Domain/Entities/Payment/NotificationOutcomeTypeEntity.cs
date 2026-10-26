using Zapper.Domain.Enums;

namespace Zapper.Domain.Entities.Payment;

public sealed class NotificationOutcomeTypeEntity
{
    public NotificationOutcome Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
