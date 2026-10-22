using Zapper.Domain.Error;

namespace Zapper.ApiCommon.Managers.ErrorMessage;

public sealed class ErrorMessageManager : IErrorMessageManager
{
    private readonly Dictionary<int, ErrorStatusEntity> statusesByCode = new();

    private readonly Dictionary<string, ErrorMessageEntity> messagesByCode = new(StringComparer.Ordinal);

    public void Populate(IReadOnlyCollection<ErrorStatusEntity> statuses, IReadOnlyCollection<ErrorMessageEntity> messages)
    {
        foreach (var status in statuses)
        {
            statusesByCode[status.StatusCode] = status;
        }

        foreach (var message in messages)
        {
            messagesByCode[message.Code] = message;
        }
    }

    public ErrorStatusEntity GetStatusByStatusCode(int statusCode) =>
        statusesByCode.TryGetValue(statusCode, out var status)
            ? status
            : throw new InvalidOperationException($"No ErrorStatus row exists for status {statusCode}.");

    public ErrorMessageEntity? GetMessageByCode(string code) =>
        messagesByCode.TryGetValue(code, out var message) ? message : null;
}
