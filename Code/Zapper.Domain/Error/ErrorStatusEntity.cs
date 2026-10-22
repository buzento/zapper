namespace Zapper.Domain.Error;

public sealed class ErrorStatusEntity
{
    public ErrorStatusEntity(int statusCode, string code, string title, string detail)
    {
        Id = Guid.CreateVersion7();
        StatusCode = statusCode;
        Code = code;
        Title = title;
        Detail = detail;
    }

    public Guid Id { get; private set; }

    public int StatusCode { get; private set; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public string Detail { get; private set; }
}
