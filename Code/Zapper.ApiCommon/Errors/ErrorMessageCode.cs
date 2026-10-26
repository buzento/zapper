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

    public const string RequestTypeRequired = nameof(RequestTypeRequired);

    public const string RequestTypeTooLong = nameof(RequestTypeTooLong);

    public const string MerchantIdRequired = nameof(MerchantIdRequired);

    public const string MerchantIdTooLong = nameof(MerchantIdTooLong);

    public const string TransactionIdRequired = nameof(TransactionIdRequired);

    public const string TransactionIdTooLong = nameof(TransactionIdTooLong);

    public const string TransactionDateTimeMalformed = nameof(TransactionDateTimeMalformed);

    public const string OrderNumberTooLong = nameof(OrderNumberTooLong);

    public const string ReconciliationIdTooLong = nameof(ReconciliationIdTooLong);

    public const string RetrievalReferenceNumberRequired = nameof(RetrievalReferenceNumberRequired);

    public const string RetrievalReferenceNumberTooLong = nameof(RetrievalReferenceNumberTooLong);

    public const string SystemsTraceAuditNumberRequired = nameof(SystemsTraceAuditNumberRequired);

    public const string SystemsTraceAuditNumberTooLong = nameof(SystemsTraceAuditNumberTooLong);

    public const string PaymentServiceRequired = nameof(PaymentServiceRequired);

    public const string PaymentServiceTooLong = nameof(PaymentServiceTooLong);

    public const string TransactionStatusRequired = nameof(TransactionStatusRequired);

    public const string TransactionStatusTooLong = nameof(TransactionStatusTooLong);

    public const string ResponseSourceRequired = nameof(ResponseSourceRequired);

    public const string ResponseSourceTooLong = nameof(ResponseSourceTooLong);

    public const string ResponseCodeRequired = nameof(ResponseCodeRequired);

    public const string ResponseCodeTooLong = nameof(ResponseCodeTooLong);

    public const string ResponseDescriptionTooLong = nameof(ResponseDescriptionTooLong);

    public const string AuthorisationCodeTooLong = nameof(AuthorisationCodeTooLong);

    public const string CommunicationProtocolRequired = nameof(CommunicationProtocolRequired);

    public const string CommunicationProtocolTooLong = nameof(CommunicationProtocolTooLong);

    public const string ConcurrentChange = nameof(ConcurrentChange);

    public const string TransactionDateTimeRequired = nameof(TransactionDateTimeRequired);

    public const string CurrencyCodeRequired = nameof(CurrencyCodeRequired);

    public const string TransactionUuidRequired = nameof(TransactionUuidRequired);

    public const string TransactionUuidTooLong = nameof(TransactionUuidTooLong);

    public const string TransactionTypeRequired = nameof(TransactionTypeRequired);

    public const string TransactionTypeTooLong = nameof(TransactionTypeTooLong);

    public const string TransactionResultCodeRequired = nameof(TransactionResultCodeRequired);

    public const string TransactionResultCodeTooLong = nameof(TransactionResultCodeTooLong);

    public const string IsApprovedRequired = nameof(IsApprovedRequired);
}
