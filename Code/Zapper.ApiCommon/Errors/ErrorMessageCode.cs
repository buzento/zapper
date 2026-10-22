namespace Zapper.ApiCommon.Errors;

public static class ErrorMessageCode
{
    public const string BadRequest = nameof(BadRequest);

    public const string Unauthorized = nameof(Unauthorized);

    public const string Forbidden = nameof(Forbidden);

    public const string NotFound = nameof(NotFound);

    public const string MethodNotAllowed = nameof(MethodNotAllowed);

    public const string RequestTooLarge = nameof(RequestTooLarge);

    public const string UnsupportedMediaType = nameof(UnsupportedMediaType);

    public const string TooManyRequests = nameof(TooManyRequests);

    public const string Internal = nameof(Internal);
}
