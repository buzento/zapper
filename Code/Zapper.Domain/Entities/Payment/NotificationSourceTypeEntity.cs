using Zapper.Domain.Enums;

namespace Zapper.Domain.Entities.Payment;

public sealed class NotificationSourceTypeEntity
{
    public NotificationSource Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
