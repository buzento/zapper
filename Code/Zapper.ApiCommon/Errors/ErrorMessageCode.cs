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

    public const string CustomerReferenceRequired = nameof(CustomerReferenceRequired);

    public const string CustomerReferenceTooLong = nameof(CustomerReferenceTooLong);

    public const string SubMerchantReferenceRequired = nameof(SubMerchantReferenceRequired);

    public const string SubMerchantReferenceTooLong = nameof(SubMerchantReferenceTooLong);

    public const string AmountBelowMinimum = nameof(AmountBelowMinimum);

    public const string CurrencyCodeTooLong = nameof(CurrencyCodeTooLong);

    public const string AppPaymentReferenceRequired = nameof(AppPaymentReferenceRequired);

    public const string AppPaymentReferenceTooLong = nameof(AppPaymentReferenceTooLong);

    public const string AppPaymentReferenceReused = nameof(AppPaymentReferenceReused);

    public const string CustomerHasNoStoredCard = nameof(CustomerHasNoStoredCard);

    public const string SubMerchantNotRegistered = nameof(SubMerchantNotRegistered);

    public const string CurrencyCodeNotSupported = nameof(CurrencyCodeNotSupported);

    public const string AmountAboveMaximum = nameof(AmountAboveMaximum);
}
