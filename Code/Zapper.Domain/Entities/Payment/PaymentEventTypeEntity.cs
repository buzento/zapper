using Zapper.Domain.Enums;

namespace Zapper.Domain.Entities.Payment;

public sealed class PaymentEventTypeEntity
{
    public PaymentEvent Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
