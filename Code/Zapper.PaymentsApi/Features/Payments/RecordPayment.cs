using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Zapper.ApiCommon.Errors;
using Zapper.ApiCommon.Validation;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Infrastructure.Persistence;

namespace Zapper.PaymentsApi.Features.Payments;

public sealed class RecordPayment
{
    // Acme's own default where CurrencyCode is left out, D-10
    private const string DefaultCurrencyCode = "ZAR";

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
             .WithValidation<RecordPaymentValidator>()
             .WithSummary("Records a payment for the app to track.")
             .Produces<RecordPaymentResponse>(201)
             .ProducesValidationProblem()
             .ProducesProblem(500)
             .AllowAnonymous();

    private static async Task<Results<Created<RecordPaymentResponse>, ProblemHttpResult>> HandleAsync(
        RecordPaymentRequest request,
        ZapperDbContext database,
        TimeProvider time,
        ILogger<RecordPayment> logger,
        CancellationToken cancellationToken)
    {
        var heldPayment = await GetPaymentByAppPaymentReferenceAsync(database, request.AppPaymentReference, cancellationToken);

        var storedCard = await database.StoredCards
            .SingleOrDefaultAsync(candidate => candidate.CustomerReference == request.CustomerReference, cancellationToken);

        var subMerchant = await database.SubMerchants
            .SingleOrDefaultAsync(candidate => candidate.MerchantReference == request.SubMerchantReference, cancellationToken);

        var currencyCode = request.CurrencyCode?.ToUpperInvariant() ?? DefaultCurrencyCode;
        var currency = await database.CurrencyTypes
            .SingleOrDefaultAsync(candidate => candidate.Code == currencyCode, cancellationToken);

        if (heldPayment is not null)
        {
            return AnswerHeldPayment(heldPayment, storedCard, subMerchant, currency, request.Amount, logger);
        }

        if (storedCard is null)
        {
            return Refuse(ErrorMessageCode.CustomerHasNoStoredCard, logger);
        }

        if (subMerchant is null)
        {
            return Refuse(ErrorMessageCode.SubMerchantNotRegistered, logger);
        }

        if (currency is null)
        {
            return Refuse(ErrorMessageCode.CurrencyCodeNotSupported, logger);
        }

        if (currency.CanRecordPayment(request.Amount) is RecordPaymentResult.AmountAboveMaximum)
        {
            return Refuse(ErrorMessageCode.AmountAboveMaximum, logger);
        }

        var payment = new PaymentEntity(storedCard, subMerchant, currency, request.Amount, request.AppPaymentReference, time);
        database.Payments.Add(payment);

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        if (outcome is SaveOutcome.AlreadyExists)
        {
            var recordedMeanwhile =
                await GetPaymentByAppPaymentReferenceAsync(database, request.AppPaymentReference, cancellationToken);

            if (recordedMeanwhile is null)
            {
                logger.LogError("A new order number was already held, so payment {PaymentId} was not recorded", payment.Id);

                return TypedResults.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    extensions: new Dictionary<string, object?> { ["code"] = ErrorMessageCode.Internal });
            }

            return AnswerHeldPayment(recordedMeanwhile, storedCard, subMerchant, currency, request.Amount, logger);
        }

        logger.LogInformation("Payment {PaymentId} was recorded", payment.Id);

        return Created(payment, currency.Code);
    }

    private static Task<PaymentEntity?> GetPaymentByAppPaymentReferenceAsync(
        ZapperDbContext database,
        string appPaymentReference,
        CancellationToken cancellationToken) =>
        database.Payments
            .SingleOrDefaultAsync(candidate => candidate.AppPaymentReference == appPaymentReference, cancellationToken);

    private static Results<Created<RecordPaymentResponse>, ProblemHttpResult> AnswerHeldPayment(
        PaymentEntity heldPayment,
        StoredCardEntity? storedCard,
        SubMerchantEntity? subMerchant,
        CurrencyTypeEntity? currency,
        long amount,
        ILogger<RecordPayment> logger) =>
        heldPayment.CompareRequest(storedCard, subMerchant, currency, amount) switch
        {
            // AlreadyRecorded means the request named the held payment's currency, so it was found
            RecordPaymentResult.AlreadyRecorded => Created(heldPayment, currency!.Code),
            _ => Refuse(ErrorMessageCode.AppPaymentReferenceReused, logger)
        };

    private static Created<RecordPaymentResponse> Created(PaymentEntity payment, string currencyCode) =>
        TypedResults.Created(
            $"/v1/payments/{payment.Id}",
            new RecordPaymentResponse(
                payment.Id,
                payment.AppPaymentReference,
                payment.GetStatus().ToString(),
                payment.Amount,
                currencyCode,
                payment.AuthorisedAmount,
                payment.ResponseCode,
                payment.ClientMessage,
                payment.ReversedAmount,
                payment.RefundedAmount));

    private static ProblemHttpResult Refuse(string code, ILogger<RecordPayment> logger)
    {
        logger.LogInformation("Recording a payment was refused with {Code}", code);

        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}

public sealed record RecordPaymentRequest(
    string CustomerReference,
    string SubMerchantReference,
    long Amount,
    string AppPaymentReference,
    string? CurrencyCode = null);

public sealed record RecordPaymentResponse(
    Guid Id,
    string AppPaymentReference,
    string Status,
    long Amount,
    string CurrencyCode,
    long? AuthorisedAmount,
    string? ResponseCode,
    string? ClientMessage,
    long ReversedAmount,
    long RefundedAmount);

public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentValidator()
    {
        RuleFor(request => request.CustomerReference)
            .NotEmpty().WithMessage(ErrorMessageCode.CustomerReferenceRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.CustomerReferenceTooLong);

        RuleFor(request => request.SubMerchantReference)
            .NotEmpty().WithMessage(ErrorMessageCode.SubMerchantReferenceRequired)
            .MaximumLength(15).WithMessage(ErrorMessageCode.SubMerchantReferenceTooLong);

        // the highest amount depends on the currency, which only the stored list knows, so the Domain checks it
        RuleFor(request => request.Amount)
            .GreaterThanOrEqualTo(1).WithMessage(ErrorMessageCode.AmountBelowMinimum);

        RuleFor(request => request.CurrencyCode)
            .MaximumLength(3).WithMessage(ErrorMessageCode.CurrencyCodeTooLong);

        RuleFor(request => request.AppPaymentReference)
            .NotEmpty().WithMessage(ErrorMessageCode.AppPaymentReferenceRequired)
            .MaximumLength(50).WithMessage(ErrorMessageCode.AppPaymentReferenceTooLong);
    }
}
