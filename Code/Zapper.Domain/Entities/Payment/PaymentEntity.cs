using System.Security.Cryptography;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Enums;
using Zapper.Domain.Models;

namespace Zapper.Domain.Entities.Payment;

public sealed class PaymentEntity
{
    private const string OrderNumberCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int OrderNumberLength = 12;

    // Appendix A puts the outcome in the response code, and 00 is the one that approves
    private const string ApprovedResponseCode = "00";

    private const int UnexpectedFailuresBeforeQuery = 5;

    // Appendix A's values that a Payment Advice is read by; any other is recorded and changes nothing, R-9
    private const string PaymentRequestType = "Payment";

    private const string SuccessTransactionStatus = "Success";

    private const string FailureTransactionStatus = "Failure";

    // Appendix A's values that name the two events of a Transaction Status; any other changes nothing, R-15
    private const string ReversalTransactionType = "Reversal";

    private const string RefundTransactionType = "Refund";

    private const int MostSendsNeverReceived = 3;

    private static readonly TimeSpan TimeToFinalAnswer = TimeSpan.FromHours(24);

    private static readonly TimeSpan FirstQueryGap = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan LongestQueryGap = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan RetryAfterWhenMissing = TimeSpan.FromSeconds(1);

    private readonly List<PaymentChangeEntity> changes = [];

    private readonly List<PaymentEventEntity> events = [];

    private readonly List<NotificationEntity> notifications = [];

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

    public IReadOnlyCollection<PaymentEventEntity> Events => events;

    public IReadOnlyCollection<NotificationEntity> Notifications => notifications;

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
        PaymentState.Reversed => PaymentStatus.Reversed,
        PaymentState.Refunded => PaymentStatus.Refunded,
        PaymentState.PartiallyRefunded => PaymentStatus.PartiallyRefunded,
        _ => PaymentStatus.Pending
    };

    public NextCallResult DecideNextCall(TimeProvider time)
    {
        var now = time.GetUtcNow();

        // a notification that made the payment Unresolved left its due time alone, since that is the worker's lease,
        // so the worker stops here
        if (State == PaymentState.Unresolved)
        {
            NextActionAt = null;

            return NextCallResult.Unresolved;
        }

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

        // a notification made the payment Unresolved while this call was made, and an answer never settles it
        if (State == PaymentState.Unresolved)
        {
            NextActionAt = null;

            return AcmeAnswerResult.Unresolved;
        }

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

        // a notification made the payment Unresolved while this call was made, and an answer never settles it
        if (State == PaymentState.Unresolved)
        {
            NextActionAt = null;

            return AcmeAnswerResult.Unresolved;
        }

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

    public RecordPaymentAdviceResult RecordPaymentAdvice(
        NotificationEntity notification,
        AcmePaymentAdvice advice,
        IReadOnlyCollection<NotificationEntity> heldWithSameIdentity,
        string accountMerchantId,
        TimeProvider time)
    {
        // an advice for another account at Acme is not about this payment, R-6
        if (!string.Equals(advice.MerchantId, accountMerchantId, StringComparison.OrdinalIgnoreCase))
        {
            notification.RecordUnmatched();

            return RecordPaymentAdviceResult.Unmatched;
        }

        var now = time.GetUtcNow();

        if (State == PaymentState.Unresolved
            || advice.RequestType != PaymentRequestType
            || advice.TransactionStatus is not (SuccessTransactionStatus or FailureTransactionStatus))
        {
            KeepNotification(notification, NotificationOutcome.Recorded);

            return RecordPaymentAdviceResult.Recorded;
        }

        if (IsContradictedByHeld(notification, heldWithSameIdentity) || !MatchesPaymentAdvice(advice))
        {
            KeepNotification(notification, NotificationOutcome.Contradiction);
            ChangeState(PaymentState.Unresolved, PaymentChangeSource.PaymentAdvice, null, notification.Id, now);

            return RecordPaymentAdviceResult.Contradiction;
        }

        // a Payment Advice never sets the outcome, so one that comes first waits for the worker's answer, R-8
        if (GetOutcome() is null)
        {
            KeepNotification(notification, NotificationOutcome.Held);

            return RecordPaymentAdviceResult.Held;
        }

        if (CompareAdvice(notification, null, now))
        {
            KeepNotification(notification, NotificationOutcome.Confirmed);

            return RecordPaymentAdviceResult.Confirmed;
        }

        KeepNotification(notification, NotificationOutcome.Contradiction);

        return RecordPaymentAdviceResult.Contradiction;
    }

    public RecordTransactionStatusResult RecordTransactionStatus(
        NotificationEntity notification,
        AcmeTransactionStatus status,
        IReadOnlyCollection<NotificationEntity> heldWithSameIdentity,
        TimeProvider time)
    {
        var now = time.GetUtcNow();
        PaymentEvent? paymentEvent = status.TransactionType switch
        {
            ReversalTransactionType => PaymentEvent.Reversal,
            RefundTransactionType => PaymentEvent.Refund,
            _ => null
        };

        if (paymentEvent is null)
        {
            KeepNotification(notification, NotificationOutcome.Recorded);

            return RecordTransactionStatusResult.Recorded;
        }

        if (IsContradictedByHeld(notification, heldWithSameIdentity))
        {
            KeepNotification(notification, NotificationOutcome.Contradiction);
            ChangeState(PaymentState.Unresolved, PaymentChangeSource.TransactionStatus, null, notification.Id, now);

            return RecordTransactionStatusResult.Contradiction;
        }

        var newEvent = new PaymentEventEntity(
            Id, notification.Id, paymentEvent.Value, status.Amount, status.CurrencyCode, status.AcmeProcessedAt);
        events.Add(newEvent);

        // the switch did not apply it, or a person settles the payment, R-13
        if (!status.IsApproved || status.TransactionResultCode != ApprovedResponseCode || State == PaymentState.Unresolved)
        {
            newEvent.LeaveNotApplied();
            KeepNotification(notification, NotificationOutcome.Recorded);

            return RecordTransactionStatusResult.Recorded;
        }

        if (!string.Equals(status.CurrencyCode, GetCurrencyCode(), StringComparison.OrdinalIgnoreCase))
        {
            ContradictEvent(newEvent, null, now);
            KeepNotification(notification, NotificationOutcome.Contradiction);

            return RecordTransactionStatusResult.Contradiction;
        }

        // an event that comes before the outcome waits for the worker's answer, R-12
        if (GetOutcome() is not { } outcome)
        {
            KeepNotification(notification, NotificationOutcome.Held);

            return RecordTransactionStatusResult.Held;
        }

        // a reversal or a refund of a payment that was declined or failed is a contradiction, R-13
        if (outcome != PaymentState.Approved)
        {
            ContradictEvent(newEvent, null, now);
            KeepNotification(notification, NotificationOutcome.Contradiction);

            return RecordTransactionStatusResult.Contradiction;
        }

        if (!ApplyEvent(newEvent, null, now))
        {
            KeepNotification(notification, NotificationOutcome.Contradiction);

            return RecordTransactionStatusResult.Contradiction;
        }

        KeepNotification(notification, NotificationOutcome.Applied);

        return RecordTransactionStatusResult.Applied;
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
        ChangeState(state, source, acmeInteractionId, null, now);
        NextActionAt = null;

        ApplyHeldFacts(acmeInteractionId, now);

        return State == PaymentState.Unresolved ? AcmeAnswerResult.Unresolved : result;
    }

    // the advices and events that came before the outcome are decided now, the advices first, then the events in
    // the order Acme Switch processed them, R-8 and R-12
    private void ApplyHeldFacts(Guid? acmeInteractionId, DateTimeOffset now)
    {
        var outcome = GetOutcome();
        var heldAdvices = notifications
            .Where(notification => notification.Source == NotificationSource.PaymentAdvice
                                   && notification.Outcome == NotificationOutcome.Held)
            .OrderBy(notification => notification.ReceivedAt)
            .ToList();

        // once one advice contradicts the outcome a person settles the payment, so the others wait for them
        foreach (var heldAdvice in heldAdvices.TakeWhile(_ => State != PaymentState.Unresolved))
        {
            CompareAdvice(heldAdvice, acmeInteractionId, now);
        }

        var heldEvents = events
            .Where(paymentEvent => paymentEvent.State == PaymentEventState.Held)
            .OrderBy(paymentEvent => paymentEvent.AcmeProcessedAt)
            .ToList();

        foreach (var heldEvent in heldEvents)
        {
            if (outcome != PaymentState.Approved)
            {
                ContradictEvent(heldEvent, acmeInteractionId, now);
            }
            else if (State == PaymentState.Unresolved)
            {
                heldEvent.LeaveNotApplied();
            }
            else
            {
                ApplyEvent(heldEvent, acmeInteractionId, now);
            }
        }
    }

    // an advice agrees when it says the outcome Acme's answer set, and for a decline the same response code; an
    // agreement is recorded as a change that leaves the state as it was, R-8
    private bool CompareAdvice(NotificationEntity advice, Guid? acmeInteractionId, DateTimeOffset now)
    {
        var said = advice.TransactionStatus == FailureTransactionStatus
            ? PaymentState.Failed
            : advice.ResponseCode == ApprovedResponseCode
                ? PaymentState.Approved
                : PaymentState.Declined;
        var outcome = GetOutcome();

        if (said == outcome && (outcome != PaymentState.Declined || advice.ResponseCode == ResponseCode))
        {
            RecordChange(State, PaymentChangeSource.PaymentAdvice, acmeInteractionId, advice.Id, now);

            return true;
        }

        ChangeState(PaymentState.Unresolved, PaymentChangeSource.PaymentAdvice, acmeInteractionId, advice.Id, now);

        return false;
    }

    // a reversal takes exactly the authorised amount, once and before any refund; refunds may be several, and
    // together with any reversal never pass the authorised amount, R-10 and R-11
    private bool ApplyEvent(PaymentEventEntity paymentEvent, Guid? acmeInteractionId, DateTimeOffset now)
    {
        var authorisedAmount = AuthorisedAmount ?? 0;
        var isAllowed = paymentEvent.Event == PaymentEvent.Reversal
            ? ReversedAmount == 0 && RefundedAmount == 0 && paymentEvent.Amount == authorisedAmount
            : ReversedAmount + RefundedAmount + paymentEvent.Amount <= authorisedAmount;

        if (!isAllowed)
        {
            ContradictEvent(paymentEvent, acmeInteractionId, now);

            return false;
        }

        PaymentState state;

        if (paymentEvent.Event == PaymentEvent.Reversal)
        {
            ReversedAmount = paymentEvent.Amount;
            state = PaymentState.Reversed;
        }
        else
        {
            RefundedAmount += paymentEvent.Amount;
            state = RefundedAmount == authorisedAmount ? PaymentState.Refunded : PaymentState.PartiallyRefunded;
        }

        paymentEvent.Apply(now);
        RecordChange(state, PaymentChangeSource.TransactionStatus, acmeInteractionId, paymentEvent.NotificationId, now);

        return true;
    }

    // a notification never moves the due time, since it is the worker's lease
    private void ContradictEvent(PaymentEventEntity paymentEvent, Guid? acmeInteractionId, DateTimeOffset now)
    {
        paymentEvent.MarkContradiction();
        ChangeState(
            PaymentState.Unresolved, PaymentChangeSource.TransactionStatus, acmeInteractionId, paymentEvent.NotificationId, now);
    }

    // the payment keeps every notification it was matched to, so an advice that is held is there when an answer
    // sets the outcome
    private void KeepNotification(NotificationEntity notification, NotificationOutcome outcome)
    {
        notification.RecordForPayment(Id, outcome);
        notifications.Add(notification);
    }

    // the same identity held with other mapped fields is a contradiction; the same mapped fields are a duplicate,
    // which the unique index refuses when the notification is saved, R-4 and R-5
    private static bool IsContradictedByHeld(
        NotificationEntity notification,
        IReadOnlyCollection<NotificationEntity> heldWithSameIdentity) =>
        heldWithSameIdentity.Any(held => !held.ContentHash.AsSpan().SequenceEqual(notification.ContentHash));

    // the Retrieval Reference Number Acme sends is our order number, R-7
    private bool MatchesPaymentAdvice(AcmePaymentAdvice advice) =>
        advice.Amount == Amount
        && (advice.CurrencyCode is null || string.Equals(advice.CurrencyCode, GetCurrencyCode(), StringComparison.OrdinalIgnoreCase))
        && (advice.OrderNumber is null || advice.OrderNumber == OrderNumber)
        && advice.RetrievalReferenceNumber == OrderNumber;

    // the outcome Acme's first final answer set, which a reversal or a refund leaves an approval
    private PaymentState? GetOutcome() => State switch
    {
        PaymentState.Approved or PaymentState.Reversed or PaymentState.Refunded or PaymentState.PartiallyRefunded =>
            PaymentState.Approved,
        PaymentState.Declined => PaymentState.Declined,
        PaymentState.Failed => PaymentState.Failed,
        _ => null
    };

    // each currency's name is its ISO code, as Payment.CurrencyType holds it
    private string GetCurrencyCode() => Currency.ToString().ToUpperInvariant();

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
        ChangeState(PaymentState.Failed, source, acmeInteractionId, null, now);
        NextActionAt = null;

        return result;
    }

    private AcmeAnswerResult MakeUnresolved(PaymentChangeSource source, Guid? acmeInteractionId, DateTimeOffset now)
    {
        ChangeState(PaymentState.Unresolved, source, acmeInteractionId, null, now);
        NextActionAt = null;

        return AcmeAnswerResult.Unresolved;
    }

    private AcmeAnswerResult SendAgain(Guid? acmeInteractionId, DateTimeOffset now)
    {
        ChangeState(PaymentState.Recorded, PaymentChangeSource.Query, acmeInteractionId, null, now);
        NextActionAt = now;

        return AcmeAnswerResult.SendAgain;
    }

    private AcmeAnswerResult QueryLater(PaymentChangeSource source, Guid? acmeInteractionId, DateTimeOffset now)
    {
        if (source == PaymentChangeSource.Query)
        {
            QueryCount++;
        }

        ChangeState(PaymentState.OutcomeUnknown, source, acmeInteractionId, null, now);
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
        Guid? notificationId,
        DateTimeOffset now)
    {
        if (state == State)
        {
            return;
        }

        RecordChange(state, source, acmeInteractionId, notificationId, now);
    }

    // a confirmation, or an applied event that leaves the state as it was, is recorded as well
    private void RecordChange(
        PaymentState state,
        PaymentChangeSource source,
        Guid? acmeInteractionId,
        Guid? notificationId,
        DateTimeOffset now)
    {
        changes.Add(new PaymentChangeEntity(Id, State, state, source, acmeInteractionId, notificationId, now));
        State = state;
    }
}
