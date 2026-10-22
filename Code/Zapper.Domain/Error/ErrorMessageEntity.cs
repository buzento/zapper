namespace Zapper.Domain.Error;

public sealed class ErrorMessageEntity
{
    public ErrorMessageEntity(string code, string detail)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Detail = detail;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Detail { get; private set; }
}
