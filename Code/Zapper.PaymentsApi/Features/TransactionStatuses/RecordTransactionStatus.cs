using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Zapper.ApiCommon.Errors;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.ApiCommon.Security;
using Zapper.ApiCommon.Validation;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Domain.Models;
using Zapper.Infrastructure.Persistence;

namespace Zapper.PaymentsApi.Features.TransactionStatuses;

public sealed class RecordTransactionStatus
{
    // Acme sends South African time with no offset, so it is read at UTC+02:00 rather than the server's zone
    internal const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    // the values Appendix A names; any other is recorded and logged at Warning
    private static readonly string[] KnownTransactionTypes = ["Reversal", "Refund"];

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithValidation<RecordTransactionStatusValidator>()
             .WithSummary("Records a Transaction Status notification from Acme Switch.")
             .Produces(200)
             .ProducesValidationProblem()
             .ProducesProblem(401)
             .ProducesProblem(500)
             .RequireAuthorization(Authentication.SendTransactionStatus)
             .RequireRateLimiting(RateLimiterConfigureOptions.ProviderPolicy);

    private static async Task<Results<Ok, ProblemHttpResult>> HandleAsync(
        RecordTransactionStatusRequest request,
        ZapperDbContext database,
        ICryptographyManager cryptographyManager,
        TimeProvider time,
        ILogger<RecordTransactionStatus> logger,
        CancellationToken cancellationToken)
    {
        // the validator has run, so every value it requires is present
        var status = new AcmeTransactionStatus(
            request.TransactionUuid,
            request.TransactionType,
            ReadTime(request.TransactionDateTime),
            request.RetrievalReferenceNumber,
            request.Amount,
            request.CurrencyCode,
            request.TransactionResultCode,
            request.IsApproved!.Value);

        var heldWithSameIdentity = await database.Notifications
            .Where(held => held.Source == NotificationSource.TransactionStatus
                           && held.AcmeReference == request.TransactionUuid)
            .ToListAsync(cancellationToken);

        var payment = await database.Payments
            .AsTracking()
            .Include(candidate => candidate.Events)
            .SingleOrDefaultAsync(
                candidate => candidate.OrderNumber == request.RetrievalReferenceNumber,
                cancellationToken);

        var notification = new NotificationEntity(
            NotificationSource.TransactionStatus,
            request.TransactionUuid,
            null,
            cryptographyManager.GetSha256Hash(status.GetContent()),
            JsonSerializer.Serialize(request),
            null,
            null,
            Activity.Current?.TraceId.ToHexString() ?? string.Empty,
            time);

        RecordTransactionStatusResult? result = null;

        if (payment is null)
        {
            notification.RecordUnmatched();
        }
        else
        {
            result = payment.RecordTransactionStatus(notification, status, heldWithSameIdentity, time);

            // the payment's row is written even where nothing on it changed, so its version moves: the worker's save
            // then meets Conflict and reloads the payment with this notification, and this save meets the worker's
            database.Entry(payment).Property(candidate => candidate.State).IsModified = true;
        }

        database.Notifications.Add(notification);

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        if (outcome is SaveOutcome.Conflict)
        {
            logger.LogWarning(
                "Transaction Status {NotificationId} was not stored, because payment {PaymentId} changed meanwhile",
                notification.Id,
                payment?.Id);

            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["code"] = ErrorMessageCode.ConcurrentChange });
        }

        if (outcome is SaveOutcome.AlreadyExists)
        {
            logger.LogInformation("A Transaction Status for payment {PaymentId} was a duplicate", payment?.Id);

            return TypedResults.Ok();
        }

        Log(logger, result, status, notification.Id, payment?.Id);

        return TypedResults.Ok();
    }

    // stored as UTC, the only offset a timestamp with time zone takes
    private static DateTimeOffset ReadTime(string transactionDateTime) =>
        new DateTimeOffset(
                DateTime.ParseExact(transactionDateTime, TimeFormat, CultureInfo.InvariantCulture),
                SouthAfricanOffset)
            .ToUniversalTime();

    private static void Log(
        ILogger<RecordTransactionStatus> logger,
        RecordTransactionStatusResult? result,
        AcmeTransactionStatus status,
        Guid notificationId,
        Guid? paymentId)
    {
        switch (result)
        {
            case null:
                logger.LogError("Transaction Status {NotificationId} matched no payment", notificationId);
                break;
            case RecordTransactionStatusResult.Contradiction:
                logger.LogError(
                    "Transaction Status {NotificationId} contradicts payment {PaymentId}", notificationId, paymentId);
                break;
            case RecordTransactionStatusResult.Recorded when !KnownTransactionTypes.Contains(status.TransactionType):
                logger.LogWarning(
                    "Transaction Status {NotificationId} for payment {PaymentId} carries a value Appendix A does not name",
                    notificationId,
                    paymentId);
                break;
            default:
                logger.LogInformation(
                    "Transaction Status {NotificationId} for payment {PaymentId} was {Result}",
                    notificationId,
                    paymentId,
                    result);
                break;
        }
    }
}

public sealed record RecordTransactionStatusRequest(
    [property: JsonPropertyName("transactionUUID")] string TransactionUuid,
    [property: JsonPropertyName("transactionType")] string TransactionType,
    [property: JsonPropertyName("transactionDateTime")] string TransactionDateTime,
    [property: JsonPropertyName("rrn")] string RetrievalReferenceNumber,
    [property: JsonPropertyName("transactionAmount")] long Amount,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("txResultCode")] string TransactionResultCode,
    [property: JsonPropertyName("isApproved")] bool? IsApproved = null);

public sealed class RecordTransactionStatusValidator : AbstractValidator<RecordTransactionStatusRequest>
{
    public RecordTransactionStatusValidator()
    {
        RuleFor(request => request.TransactionUuid)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionUuidRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.TransactionUuidTooLong);

        RuleFor(request => request.TransactionType)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionTypeRequired)
            .MaximumLength(20).WithMessage(ErrorMessageCode.TransactionTypeTooLong);

        RuleFor(request => request.TransactionDateTime)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionDateTimeRequired)
            .Must(BeAcmeTime).WithMessage(ErrorMessageCode.TransactionDateTimeMalformed);

        RuleFor(request => request.RetrievalReferenceNumber)
            .NotEmpty().WithMessage(ErrorMessageCode.RetrievalReferenceNumberRequired)
            .MaximumLength(12).WithMessage(ErrorMessageCode.RetrievalReferenceNumberTooLong);

        RuleFor(request => request.Amount)
            .GreaterThanOrEqualTo(1).WithMessage(ErrorMessageCode.AmountBelowMinimum);

        RuleFor(request => request.CurrencyCode)
            .NotEmpty().WithMessage(ErrorMessageCode.CurrencyCodeRequired)
            .MaximumLength(3).WithMessage(ErrorMessageCode.CurrencyCodeTooLong);

        RuleFor(request => request.TransactionResultCode)
            .NotEmpty().WithMessage(ErrorMessageCode.TransactionResultCodeRequired)
            .MaximumLength(3).WithMessage(ErrorMessageCode.TransactionResultCodeTooLong);

        RuleFor(request => request.IsApproved)
            .NotNull().WithMessage(ErrorMessageCode.IsApprovedRequired);
    }

    // exactly the shape Appendix A writes, with no offset, so a time is never read in the server's zone
    private static bool BeAcmeTime(string transactionDateTime) =>
        DateTime.TryParseExact(
            transactionDateTime,
            RecordTransactionStatus.TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
}
