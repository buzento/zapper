using Zapper.Domain.Entities.Error;

namespace Zapper.ApiCommon.Managers.ErrorMessage;

public interface IErrorMessageManager
{
    void Populate(IReadOnlyCollection<ErrorStatusEntity> statuses, IReadOnlyCollection<ErrorMessageEntity> messages);

    ErrorStatusEntity GetStatusByStatusCode(int statusCode);

    ErrorMessageEntity? GetMessageByCode(string code);
}
