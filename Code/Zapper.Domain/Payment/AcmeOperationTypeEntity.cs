namespace Zapper.Domain.Payment;

public enum AcmeOperation
{
    Send = 1,
    Query = 2
}

public sealed class AcmeOperationTypeEntity
{
    public AcmeOperation Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
