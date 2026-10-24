using System.Security.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;

namespace Zapper.Domain.Payment;

public sealed class PaymentEntity
{
    private const string OrderNumberCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int OrderNumberLength = 12;

    // Appendix A puts the outcome in the response code, and 00 is the one that approves
    private const string ApprovedResponseCode = "00";

    private const int UnexpectedFailuresBeforeQuery = 5;

    private const int MostSendsNeverReceived = 3;

    private static readonly TimeSpan TimeToFinalAnswer = TimeSpan.FromHours(24);

    private static readonly TimeSpan FirstQueryGap = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan LongestQueryGap = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan RetryAfterWhenMissing = TimeSpan.FromSeconds(1);

    private readonly List<PaymentChangeEntity> changes = [];

    public PaymentEntity(
        StoredCardEntity storedCard,
        SubMerchantEntity subMerchant,
        CurrencyTypeEntity currency,
        long amount,
        string appPaymentReference,
        TimeProvider time)
    {
        var now = time.GetUtcNow();

        Id = Guid.CreateVersion7();
        AppPaymentReference = appPaymentReference;
        StoredCardId = storedCard.Id;
        SubMerchantId = subMerchant.Id;
        Currency = currency.Id;
        Amount = amount;
        OrderNumber = RandomNumberGenerator.GetString(OrderNumberCharacters, OrderNumberLength);
        CardAcceptorName = subMerchant.GetCardAcceptorName();
        MerchantCategoryCode = subMerchant.MerchantCategoryCode;
        State = PaymentState.Recorded;
        RecordedAt = now;
        NextActionAt = now;
        ReversedAmount = 0;
        RefundedAmount = 0;
    }

    // EF Core materialises a stored payment through this one, since the constructor above takes entities
    private PaymentEntity()
    {
    }

    public Guid Id { get; private set; }

    public string AppPaymentReference { get; private set; } = string.Empty;

    public Guid StoredCardId { get; private set; }

    public Guid SubMerchantId { get; private set; }

    public Currency Currency { get; private set; }

    public long Amount { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public string CardAcceptorName { get; private set; } = string.Empty;

    public string MerchantCategoryCode { get; private set; } = string.Empty;

    public PaymentState State { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public DateTimeOffset? NextActionAt { get; private set; }

    public string? ResponseSource { get; private set; }

    public string? ResponseCode { get; private set; }

    public string? ResponseDescription { get; private set; }

    public string? ClientMessage { get; private set; }

    public string? AuthorisationCode { get; private set; }

    public long? AuthorisedAmount { get; private set; }

    public string? ReconciliationId { get; private set; }

    public string? RetrievalReferenceNumber { get; private set; }

    public DateTimeOffset? AcmeTransactionAt { get; private set; }

    public long ReversedAmount { get; private set; }

    public long RefundedAmount { get; private set; }

    public int AttemptCount { get; private set; }

    public int SendCount { get; private set; }

    public int QueryCount { get; private set; }

    public int UnexpectedFailureCount { get; private set; }

    public IReadOnlyCollection<PaymentChangeEntity> Changes => changes;

    // a lookup that found nothing differs from the payment held, which always has all three
    public RecordPaymentResult CompareRequest(
        StoredCardEntity? storedCard,
        SubMerchantEntity? subMerchant,
        CurrencyTypeEntity? currency,
        long amount) =>
        storedCard?.Id == StoredCardId
        && subMerchant?.Id == SubMerchantId
        && currency?.Id == Currency
        && amount == Amount
            ? RecordPaymentResult.AlreadyRecorded
            : RecordPaymentResult.AppPaymentReferenceReused;

    // Recorded and OutcomeUnknown are both still waiting for Acme, so the app sees them as Pending
    public PaymentStatus GetStatus() => State switch
    {
        PaymentState.Approved => PaymentStatus.Approved,
        PaymentState.Declined => PaymentStatus.Declined,
        PaymentState.Failed => PaymentStatus.Failed,
        PaymentState.Unresolved => PaymentStatus.Unresolved,
        _ => PaymentStatus.Pending
    };

    public NextCallResult DecideNextCall(TimeProvider time)
    {
        var now = time.GetUtcNow();

        if (now - RecordedAt >= TimeToFinalAnswer || UnexpectedFailureCount > UnexpectedFailuresBeforeQuery)
        {
            MakeUnresolved(PaymentChangeSource.Worker, null, now);

            return NextCallResult.Unresolved;
        }

        return State == PaymentState.OutcomeUnknown || UnexpectedFailureCount == UnexpectedFailuresBeforeQuery
            ? NextCallResult.Query
            : NextCallResult.Send;
    }

    public AcmeAnswerResult RecordSendAnswer(AcmeAnswer answer, Guid? acmeInteractionId, TimeProvider time)
    {
        var now = time.GetUtcNow();

        // no call left the process, so nothing is known that could end the unexpected failures
        if (answer.Type == AcmeAnswerType.CircuitOpen)
        {
            return Postpone(answer, now);
        }

        UnexpectedFailureCount = 0;

        if (answer.Type == AcmeAnswerType.RateLimited)
        {
            return Postpone(answer, now);
        }

        SendCount++;
        QueryCount = 0;

        return answer.Type switch
        {
            AcmeAnswerType.Success or AcmeAnswerType.Failure =>
                SetOutcome(answer, PaymentChangeSource.Send, acmeInteractionId, now),
            AcmeAnswerType.Refused =>
                Refuse(answer, AcmeAnswerResult.FailedRefusedByAcme, acmeInteractionId, now),
            AcmeAnswerType.OrderNumberHeldByAnother =>
                Refuse(answer, AcmeAnswerResult.FailedOrderNumberInUse, acmeInteractionId, now),
            _ => QueryLater(PaymentChangeSource.Send, acmeInteractionId, now)
        };
    }

    public AcmeAnswerResult RecordQueryAnswer(AcmeAnswer answer, Guid? acmeInteractionId, TimeProvider time)
    {
        var now = time.GetUtcNow();

        if (answer.Type == AcmeAnswerType.CircuitOpen)
        {
            return Postpone(answer, now);
        }

        var isAfterUnexpectedFailures = UnexpectedFailureCount == UnexpectedFailuresBeforeQuery;
        UnexpectedFailureCount = 0;

        return answer.Type switch
        {
            AcmeAnswerType.Success or AcmeAnswerType.Failure =>
                SetOutcome(answer, PaymentChangeSource.Query, acmeInteractionId, now),
            AcmeAnswerType.NotFound when isAfterUnexpectedFailures =>
                Fail(AcmeAnswerResult.FailedAfterUnexpectedFailures, PaymentChangeSource.Query, acmeInteractionId, now),
            _ when isAfterUnexpectedFailures =>
                MakeUnresolved(PaymentChangeSource.Query, acmeInteractionId, now),
            AcmeAnswerType.NotFound when SendCount >= MostSendsNeverReceived =>
                Fail(AcmeAnswerResult.FailedNotSubmitted, PaymentChangeSource.Query, acmeInteractionId, now),
            AcmeAnswerType.NotFound =>
                SendAgain(acmeInteractionId, now),
            AcmeAnswerType.Refused =>
                MakeUnresolved(PaymentChangeSource.Query, acmeInteractionId, now),
            AcmeAnswerType.RateLimited =>
                Postpone(answer, now),
            _ => QueryLater(PaymentChangeSource.Query, acmeInteractionId, now)
        };
    }

    private AcmeAnswerResult SetOutcome(
        AcmeAnswer answer,
        PaymentChangeSource source,
        Guid? acmeInteractionId,
        DateTimeOffset now)
    {
        var (state, result) = answer.Type == AcmeAnswerType.Failure
            ? (PaymentState.Failed, AcmeAnswerResult.Failed)
            : answer.ResponseCode == ApprovedResponseCode
                ? (PaymentState.Approved, AcmeAnswerResult.Approved)
                : (PaymentState.Declined, AcmeAnswerResult.Declined);

        KeepAcmeOutcome(answer);

        // the authorised amount is what was taken from the card, so it is kept as sent, and 0 when none was
        AuthorisedAmount = answer.AuthorisedAmount ?? 0;
        ChangeState(state, source, acmeInteractionId, now);
        NextActionAt = null;

        return result;
    }

    private AcmeAnswerResult Refuse(
        AcmeAnswer answer,
        AcmeAnswerResult result,
        Guid? acmeInteractionId,
        DateTimeOffset now)
    {
        KeepAcmeOutcome(answer);

        return Fail(result, PaymentChangeSource.Send, acmeInteractionId, now);
    }

    // Failed means no money moved, so nothing was authorised
    private AcmeAnswerResult Fail(
        AcmeAnswerResult result,
        PaymentChangeSource source,
        Guid? acmeInteractionId,
        DateTimeOffset now)
    {
        AuthorisedAmount = 0;
        ChangeState(PaymentState.Failed, source, acmeInteractionId, now);
        NextActionAt = null;

        return result;
    }

    private AcmeAnswerResult MakeUnresolved(PaymentChangeSource source, Guid? acmeInteractionId, DateTimeOffset now)
    {
        ChangeState(PaymentState.Unresolved, source, acmeInteractionId, now);
        NextActionAt = null;

        return AcmeAnswerResult.Unresolved;
    }

    private AcmeAnswerResult SendAgain(Guid? acmeInteractionId, DateTimeOffset now)
    {
        ChangeState(PaymentState.Recorded, PaymentChangeSource.Query, acmeInteractionId, now);
        NextActionAt = now;

        return AcmeAnswerResult.SendAgain;
    }

    private AcmeAnswerResult QueryLater(PaymentChangeSource source, Guid? acmeInteractionId, DateTimeOffset now)
    {
        if (source == PaymentChangeSource.Query)
        {
            QueryCount++;
        }

        ChangeState(PaymentState.OutcomeUnknown, source, acmeInteractionId, now);
        NextActionAt = now + GetQueryGap();

        return AcmeAnswerResult.QueryLater;
    }

    private AcmeAnswerResult Postpone(AcmeAnswer answer, DateTimeOffset now)
    {
        NextActionAt = now + (answer.RetryAfter ?? RetryAfterWhenMissing);

        return AcmeAnswerResult.Postponed;
    }

    // the gap doubles with each query that left the outcome unknown, from 5 seconds to at most 5 minutes;
    // the doubling stops at 2 to the power 6, already past 5 minutes, so no count can overflow it
    private TimeSpan GetQueryGap() =>
        TimeSpan.FromTicks(Math.Min(FirstQueryGap.Ticks << Math.Min(QueryCount, 6), LongestQueryGap.Ticks));

    private void KeepAcmeOutcome(AcmeAnswer answer)
    {
        ResponseSource = answer.ResponseSource;
        ResponseCode = answer.ResponseCode;
        ResponseDescription = answer.ResponseDescription;
        ClientMessage = answer.ClientMessage;
        AuthorisationCode = answer.AuthorisationCode;
        ReconciliationId = answer.ReconciliationId;
        RetrievalReferenceNumber = answer.RetrievalReferenceNumber;
        AcmeTransactionAt = answer.AcmeTransactionAt;
    }

    private void ChangeState(
        PaymentState state,
        PaymentChangeSource source,
        Guid? acmeInteractionId,
        DateTimeOffset now)
    {
        if (state == State)
        {
            return;
        }

        changes.Add(new PaymentChangeEntity(Id, State, state, source, acmeInteractionId, now));
        State = state;
    }
}

public enum NextCallResult
{
    Send,
    Query,
    Unresolved
}
