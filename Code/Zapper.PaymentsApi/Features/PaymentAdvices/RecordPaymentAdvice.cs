using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zapper.ApiCommon.Errors;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.ApiCommon.Security;
using Zapper.ApiCommon.Validation;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Domain.Models;
using Zapper.Infrastructure.Persistence;

namespace Zapper.PaymentsApi.Features.PaymentAdvices;

public sealed class RecordPaymentAdvice
{
    // Acme sends South African time with no offset, so it is read at UTC+02:00 rather than the server's zone
    internal const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // the values Appendix A names; any other is recorded and logged at Warning
    private static readonly string[] KnownRequestTypes = ["Authorize", "Capture", "Payment"];

    private static readonly string[] KnownTransactionStatuses = ["Success", "Failure", "Other"];

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithValidation<RecordPaymentAdviceValidator>()
             .WithSummary("Records a Payment Advice notification from Acme Pay.")
             .Produces(200)
             .ProducesValidationProblem()
             .ProducesProblem(401)
             .ProducesProblem(500)
             .RequireAuthorization(Authentication.SendPaymentAdvice)
             .RequireRateLimiting(RateLimiterConfigureOptions.ProviderPolicy);

    private static async Task<Results<Ok, ProblemHttpResult>> HandleAsync(
        RecordPaymentAdviceRequest request,
        ZapperDbContext database,
        ICryptographyManager cryptographyManager,
        IOptions<AcmeNotificationOptions> acmeNotification,
        TimeProvider time,
        ILogger<RecordPaymentAdvice> logger,
        CancellationToken cancellationToken)
    {
        var advice = new AcmePaymentAdvice(
            request.RequestType,
            request.MerchantId,
            request.TransactionId,
            ReadTime(request.TransactionDateTime),
            request.OrderNumber,
            request.ReconciliationId,
            request.RetrievalReferenceNumber,
            request.SystemsTraceAuditNumber,
            request.Amount,
            request.CurrencyCode,
            request.PaymentService,
            request.TransactionStatus,
            request.ResponseSource,
            request.ResponseCode,
            request.ResponseDescription,
            request.AuthorisationCode,
            request.CommunicationProtocol);

        var heldWithSameIdentity = await database.Notifications
            .Where(held => held.Source == NotificationSource.PaymentAdvice
                           && held.AcmeReference == request.TransactionId
                           && held.RequestType == request.RequestType)
            .ToListAsync(cancellationToken);

        var payment = Guid.TryParse(request.TransactionId, out var paymentId)
            ? await LoadPaymentAsync(database, paymentId, cancellationToken)
            : null;

        var notification = new NotificationEntity(
            NotificationSource.PaymentAdvice,
            request.TransactionId,
            request.RequestType,
            cryptographyManager.GetSha256Hash(advice.GetContent()),
            JsonSerializer.Serialize(request),
            request.TransactionStatus,
            request.ResponseCode,
            Activity.Current?.TraceId.ToHexString() ?? string.Empty,
            time);

        RecordPaymentAdviceResult result;

        if (payment is null)
        {
            notification.RecordUnmatched();
            result = RecordPaymentAdviceResult.Unmatched;
        }
        else
        {
            result = payment.RecordPaymentAdvice(
                notification, advice, heldWithSameIdentity, acmeNotification.Value.MerchantId, time);

            // the payment's row is written even where nothing on it changed, so its version moves: the worker's save
            // then meets Conflict and reloads the payment with this notification, and this save meets the worker's
            database.Entry(payment).Property(candidate => candidate.State).IsModified = true;
        }

        database.Notifications.Add(notification);

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        if (outcome is SaveOutcome.Conflict)
        {
            logger.LogWarning(
                "Payment Advice {NotificationId} was not stored, because payment {PaymentId} changed meanwhile",
                notification.Id,
                payment?.Id);

            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["code"] = ErrorMessageCode.ConcurrentChange });
        }

        if (outcome is SaveOutcome.AlreadyExists)
        {
            logger.LogInformation("A Payment Advice for payment {PaymentId} was a duplicate", payment?.Id);

            return TypedResults.Ok();
        }

        Log(logger, result, advice, notification.Id, payment?.Id);

        return TypedResults.Ok();
    }

    // the payment with the advices and events that wait for its outcome, Acme Notifications R-8 and R-12
    private static Task<PaymentEntity?> LoadPaymentAsync(
        ZapperDbContext database,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        database.Payments
            .AsTracking()
            .Include(payment => payment.Events.Where(paymentEvent => paymentEvent.State == PaymentEventState.Held))
            .Include(payment => payment.Notifications.Where(
                notification => notification.Source == NotificationSource.PaymentAdvice
                                && notification.Outcome == NotificationOutcome.Held))
            .SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

    private static DateTimeOffset? ReadTime(string? transactionDateTime) =>
        transactionDateTime is null
            ? null
            : new DateTimeOffset(
                    DateTime.ParseExact(transactionDateTime, TimeFormat, CultureInfo.InvariantCulture),
                    SouthAfricanOffset)
                .ToUniversalTime();

    private static void Log(
        ILogger<RecordPaymentAdvice> logger,
        RecordPaymentAdviceResult result,
        AcmePaymentAdvice advice,
        Guid notificationId,
        Guid? paymentId)
    {
        switch (result)
        {
            case RecordPaymentAdviceResult.Unmatched:
                logger.LogError("Payment Advice {NotificationId} matched no payment", notificationId);
                break;
            case RecordPaymentAdviceResult.Contradiction:
                logger.LogError(
                    "Payment Advice {NotificationId} contradicts payment {PaymentId}", notificationId, paymentId);
                break;
            case RecordPaymentAdviceResult.Recorded
                when !KnownRequestTypes.Contains(advice.RequestType)
                     || !KnownTransactionStatuses.Contains(advice.TransactionStatus):
                logger.LogWarning(
                    "Payment Advice {NotificationId} for payment {PaymentId} carries a value Appendix A does not name",
                    notificationId,
                    paymentId);
                break;
            default:
                logger.LogInformation(
                    "Payment Advice {NotificationId} for payment {PaymentId} was {Result}",
                    notificationId,
                    paymentId,
                    result);
                break;
        }
    }
}

public sealed record RecordPaymentAdviceRequest(
    [property: JsonPropertyName("RequestType")] string RequestType,
    [property: JsonPropertyName("MerchantID")] string MerchantId,
    [property: JsonPropertyName("TransactionID")] string TransactionId,
    [property: JsonPropertyName("RetrievalReferenceNumber")] string RetrievalReferenceNumber,
    [property: JsonPropertyName("SystemsTraceAuditNumber")] string SystemsTraceAuditNumber,
    [property: JsonPropertyName("Amount")] long Amount,
    [property: JsonPropertyName("PaymentService")] string PaymentService,
    [property: JsonPropertyName("TransactionStatus")] string TransactionStatus,
    [property: JsonPropertyName("ResponseSource")] string ResponseSource,
    [property: JsonPropertyName("ResponseCode")] string ResponseCode,
    [property: JsonPropertyName("CommProtocol")] string CommunicationProtocol,
    [property: JsonPropertyName("TransactionDateTime")] string? TransactionDateTime = null,
    [property: JsonPropertyName("OrderNumber")] string? OrderNumber = null,
    [property: JsonPropertyName("ReconID")] string? ReconciliationId = null,
    [property: JsonPropertyName("CurrencyCode")] string? CurrencyCode = null,
    [property: JsonPropertyName("ResponseDescription")] string? ResponseDescription = null,
    [property: JsonPropertyName("AuthCode")] string? AuthorisationCode = null,
    [property: JsonPropertyName("MaskedCardNumber")] string? MaskedCardNumber = null);

public sealed class RecordPaymentAdviceValidator : AbstractValidator<RecordPaymentAdviceRequest>
{
    public RecordPaymentAdviceValidator()
    {
        RuleFor(request => request.RequestType)
            .NotEmpty().WithMessage(ErrorMessageCode.RequestTypeRequired)
            .MaximumLength(20).WithMessage(ErrorMessageCode.RequestTypeTooLong);

        RuleFor(request => request.MerchantId)
            .NotEmpty().WithMessage(ErrorMessageCode.MerchantIdRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.MerchantIdTooLong);

        RuleFor(request => request.TransactionId)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionIdRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.TransactionIdTooLong);

        RuleFor(request => request.TransactionDateTime)
            .Must(BeAcmeTimeWhereSent).WithMessage(ErrorMessageCode.TransactionDateTimeMalformed);

        RuleFor(request => request.OrderNumber)
            .MaximumLength(50).WithMessage(ErrorMessageCode.OrderNumberTooLong);

        RuleFor(request => request.ReconciliationId)
            .MaximumLength(50).WithMessage(ErrorMessageCode.ReconciliationIdTooLong);

        RuleFor(request => request.RetrievalReferenceNumber)
            .NotEmpty().WithMessage(ErrorMessageCode.RetrievalReferenceNumberRequired)
            .MaximumLength(12).WithMessage(ErrorMessageCode.RetrievalReferenceNumberTooLong);

        RuleFor(request => request.SystemsTraceAuditNumber)
            .NotEmpty().WithMessage(ErrorMessageCode.SystemsTraceAuditNumberRequired)
            .MaximumLength(6).WithMessage(ErrorMessageCode.SystemsTraceAuditNumberTooLong);

        RuleFor(request => request.Amount)
            .GreaterThanOrEqualTo(1).WithMessage(ErrorMessageCode.AmountBelowMinimum);

        RuleFor(request => request.CurrencyCode)
            .MaximumLength(3).WithMessage(ErrorMessageCode.CurrencyCodeTooLong);

        RuleFor(request => request.PaymentService)
            .NotEmpty().WithMessage(ErrorMessageCode.PaymentServiceRequired)
            .MaximumLength(30).WithMessage(ErrorMessageCode.PaymentServiceTooLong);

        RuleFor(request => request.TransactionStatus)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionStatusRequired)
            .MaximumLength(20).WithMessage(ErrorMessageCode.TransactionStatusTooLong);

        RuleFor(request => request.ResponseSource)
            .NotEmpty().WithMessage(ErrorMessageCode.ResponseSourceRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.ResponseSourceTooLong);

        RuleFor(request => request.ResponseCode)
            .NotEmpty().WithMessage(ErrorMessageCode.ResponseCodeRequired)
            .MaximumLength(3).WithMessage(ErrorMessageCode.ResponseCodeTooLong);

        RuleFor(request => request.ResponseDescription)
            .MaximumLength(50).WithMessage(ErrorMessageCode.ResponseDescriptionTooLong);

        RuleFor(request => request.AuthorisationCode)
            .MaximumLength(6).WithMessage(ErrorMessageCode.AuthorisationCodeTooLong);

        RuleFor(request => request.CommunicationProtocol)
            .NotEmpty().WithMessage(ErrorMessageCode.CommunicationProtocolRequired)
            .MaximumLength(4).WithMessage(ErrorMessageCode.CommunicationProtocolTooLong);
    }

    // exactly the shape Appendix A writes, with no offset, so a time is never read in the server's zone
    private static bool BeAcmeTimeWhereSent(string? transactionDateTime) =>
        transactionDateTime is null
        || DateTime.TryParseExact(
            transactionDateTime,
            RecordPaymentAdvice.TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
}
