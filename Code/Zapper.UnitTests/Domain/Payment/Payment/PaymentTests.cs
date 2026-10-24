using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Domain.Payment.Payment;

public sealed class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly StoredCardEntity storedCard = new("CUS-0001", [1, 2, 3], "cus-0001@example.test");

    private readonly SubMerchantEntity subMerchant = new("ZAP0000001", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");

    private readonly CurrencyTypeEntity currency = new(Currency.Zar, "ZAR", "R", 100000000);

    [Fact]
    public void Constructor_NewPayment_RecordsRequestAndIdentityAtAcme()
    {
        var time = new FakeTimeProvider(Now);

        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", time);

        payment.Id.ShouldNotBe(Guid.Empty);
        payment.Id.Version.ShouldBe(7);
        payment.AppPaymentReference.ShouldBe("APP-0001");
        payment.StoredCardId.ShouldBe(storedCard.Id);
        payment.SubMerchantId.ShouldBe(subMerchant.Id);
        payment.Currency.ShouldBe(Currency.Zar);
        payment.Amount.ShouldBe(1999);
        payment.OrderNumber.ShouldMatch("^[A-Z0-9]{12}$");
    }

    [Fact]
    public void Constructor_NewPayment_KeepsStatementNameAndCategoryCode()
    {
        var time = new FakeTimeProvider(Now);

        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", time);

        payment.CardAcceptorName.ShouldBe("ZAP*CAFE DEL SOL       CAPE TOWN    WCZA");
        payment.MerchantCategoryCode.ShouldBe("5812");
    }

    [Fact]
    public void Constructor_NewPayment_IsRecordedAndDueAtOnce()
    {
        var time = new FakeTimeProvider(Now);

        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", time);

        payment.State.ShouldBe(PaymentState.Recorded);
        payment.RecordedAt.ShouldBe(Now);
        payment.NextActionAt.ShouldBe(Now);
        payment.ResponseCode.ShouldBeNull();
        payment.ClientMessage.ShouldBeNull();
        payment.AuthorisedAmount.ShouldBeNull();
        payment.ReversedAmount.ShouldBe(0);
        payment.RefundedAmount.ShouldBe(0);
    }

    [Fact]
    public void Constructor_TwoPayments_MakeDifferentOrderNumbers()
    {
        var time = new FakeTimeProvider(Now);
        var first = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", time);

        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0002", time);

        payment.OrderNumber.ShouldNotBe(first.OrderNumber);
    }

    [Fact]
    public void CompareRequest_SameDetails_ReturnsAlreadyRecorded()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.CompareRequest(storedCard, subMerchant, currency, 1999);

        result.ShouldBe(RecordPaymentResult.AlreadyRecorded);
    }

    [Fact]
    public void CompareRequest_DifferentStoredCard_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));
        var otherStoredCard = new StoredCardEntity("CUS-0002", [4, 5, 6], "cus-0002@example.test");

        var result = payment.CompareRequest(otherStoredCard, subMerchant, currency, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_NoStoredCard_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.CompareRequest(null, subMerchant, currency, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_DifferentSubMerchant_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));
        var otherSubMerchant = new SubMerchantEntity("ZAP0000002", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");

        var result = payment.CompareRequest(storedCard, otherSubMerchant, currency, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_NoSubMerchant_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.CompareRequest(storedCard, null, currency, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_DifferentCurrency_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));
        var otherCurrency = new CurrencyTypeEntity(Currency.Usd, "USD", "$", 5000000);

        var result = payment.CompareRequest(storedCard, subMerchant, otherCurrency, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_NoCurrency_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.CompareRequest(storedCard, subMerchant, null, 1999);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void CompareRequest_DifferentAmount_ReturnsAppPaymentReferenceReused()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.CompareRequest(storedCard, subMerchant, currency, 2500);

        result.ShouldBe(RecordPaymentResult.AppPaymentReferenceReused);
    }

    [Fact]
    public void GetStatus_RecordedPayment_ReturnsPending()
    {
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0001", new FakeTimeProvider(Now));

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void GetStatus_OutcomeUnknown_ReturnsPending()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void GetStatus_Approved_ReturnsApproved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Approved(1999, time), null, time);

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Approved);
    }

    [Fact]
    public void GetStatus_Declined_ReturnsDeclined()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Declined(null, time), null, time);

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Declined);
    }

    [Fact]
    public void GetStatus_Failed_ReturnsFailed()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Failure(time), null, time);

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Failed);
    }

    [Fact]
    public void GetStatus_Unresolved_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(Refused(), null, time);

        var result = payment.GetStatus();

        result.ShouldBe(PaymentStatus.Unresolved);
    }

    [Fact]
    public void DecideNextCall_NewPayment_ReturnsSend()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Send);
    }

    [Fact]
    public void DecideNextCall_OutcomeUnknown_ReturnsQuery()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Query);
    }

    [Fact]
    public void DecideNextCall_AfterNotFound_ReturnsSend()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Send);
    }

    [Fact]
    public void DecideNextCall_FiveUnexpectedFailures_ReturnsQuery()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 5);

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Query);
    }

    [Fact]
    public void DecideNextCall_SixUnexpectedFailures_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 6);

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void DecideNextCall_JustUnderTwentyFourHours_ReturnsQuery()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        time.Advance(TimeSpan.FromHours(24) - TimeSpan.FromMilliseconds(1));

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Query);
    }

    [Fact]
    public void DecideNextCall_TwentyFourHoursAfterRecorded_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        time.Advance(TimeSpan.FromHours(24));

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
        var change = payment.Changes.Last();
        change.FromState.ShouldBe(PaymentState.OutcomeUnknown);
        change.ToState.ShouldBe(PaymentState.Unresolved);
        change.Source.ShouldBe(PaymentChangeSource.Worker);
        change.AcmeInteractionId.ShouldBeNull();
        change.ChangedAt.ShouldBe(time.GetUtcNow());
    }

    [Fact]
    public void RecordSendAnswer_Approved_ReturnsApproved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        var acmeInteractionId = Guid.CreateVersion7();

        var result = payment.RecordSendAnswer(Approved(1999, time), acmeInteractionId, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.State.ShouldBe(PaymentState.Approved);
        payment.NextActionAt.ShouldBeNull();
        payment.SendCount.ShouldBe(1);
        payment.ResponseSource.ShouldBe("Acquirer");
        payment.ResponseCode.ShouldBe("00");
        payment.ResponseDescription.ShouldBe("Approved or completed successfully");
        payment.ClientMessage.ShouldBe("Approved.");
        payment.AuthorisationCode.ShouldBe("238699");
        payment.AuthorisedAmount.ShouldBe(1999);
        payment.ReconciliationId.ShouldBe("MR747891457");
        payment.RetrievalReferenceNumber.ShouldBe("MR43851259");
        payment.AcmeTransactionAt.ShouldBe(Now);
        var change = payment.Changes.ShouldHaveSingleItem();
        change.PaymentId.ShouldBe(payment.Id);
        change.FromState.ShouldBe(PaymentState.Recorded);
        change.ToState.ShouldBe(PaymentState.Approved);
        change.Source.ShouldBe(PaymentChangeSource.Send);
        change.AcmeInteractionId.ShouldBe(acmeInteractionId);
    }

    [Fact]
    public void RecordSendAnswer_ApprovedBelowAmountSent_ReturnsApprovedWithAuthorisedAmount()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Approved(1500, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.AuthorisedAmount.ShouldBe(1500);
    }

    [Fact]
    public void RecordSendAnswer_ApprovedAboveAmountSent_ReturnsApprovedWithAuthorisedAmount()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Approved(2500, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.AuthorisedAmount.ShouldBe(2500);
    }

    [Fact]
    public void RecordSendAnswer_DeclinedWithNoAuthorisedAmount_ReturnsDeclinedWithNothingAuthorised()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Declined(null, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Declined);
        payment.State.ShouldBe(PaymentState.Declined);
        payment.ResponseCode.ShouldBe("51");
        payment.AuthorisedAmount.ShouldBe(0);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordSendAnswer_DeclinedWithAuthorisedAmount_ReturnsDeclinedWithAmountAsSent()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Declined(100, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Declined);
        payment.State.ShouldBe(PaymentState.Declined);
        payment.AuthorisedAmount.ShouldBe(100);
    }

    [Fact]
    public void RecordSendAnswer_Failure_ReturnsFailed()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Failure(time), null, time);

        result.ShouldBe(AcmeAnswerResult.Failed);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("91");
        payment.AuthorisedAmount.ShouldBe(0);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordSendAnswer_Refused_ReturnsFailedRefusedByAcme()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Refused(), null, time);

        result.ShouldBe(AcmeAnswerResult.FailedRefusedByAcme);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("E01");
        payment.ClientMessage.ShouldBe("We could not process that payment.");
        payment.NextActionAt.ShouldBeNull();
        payment.Changes.ShouldHaveSingleItem().Source.ShouldBe(PaymentChangeSource.Send);
    }

    [Fact]
    public void RecordSendAnswer_OrderNumberHeldByAnother_ReturnsFailedOrderNumberInUse()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.OrderNumberHeldByAnother, "Application", "E09", "Retrieval Reference Number already in use", "We could not process that payment."),
            null,
            time);

        result.ShouldBe(AcmeAnswerResult.FailedOrderNumberInUse);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBe("E09");
        payment.NextActionAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(AcmeAnswerType.TimedOut)]
    [InlineData(AcmeAnswerType.ConnectionFailed)]
    [InlineData(AcmeAnswerType.Degraded)]
    [InlineData(AcmeAnswerType.Unreadable)]
    [InlineData(AcmeAnswerType.OrderNumberHeldByThis)]
    [InlineData(AcmeAnswerType.Processing)]
    public void RecordSendAnswer_OutcomeUnknown_ReturnsQueryLaterInFiveSeconds(AcmeAnswerType type)
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        var acmeInteractionId = Guid.CreateVersion7();

        var result = payment.RecordSendAnswer(Answer(type), acmeInteractionId, time);

        result.ShouldBe(AcmeAnswerResult.QueryLater);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.SendCount.ShouldBe(1);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(5));
        var change = payment.Changes.ShouldHaveSingleItem();
        change.FromState.ShouldBe(PaymentState.Recorded);
        change.ToState.ShouldBe(PaymentState.OutcomeUnknown);
        change.Source.ShouldBe(PaymentChangeSource.Send);
        change.AcmeInteractionId.ShouldBe(acmeInteractionId);
    }

    [Fact]
    public void RecordSendAnswer_RateLimited_ReturnsPostponedUntilRetryAfter()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.RateLimited, retryAfter: TimeSpan.FromSeconds(2)), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.SendCount.ShouldBe(0);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(2));
        payment.Changes.ShouldBeEmpty();
    }

    [Fact]
    public void RecordSendAnswer_RateLimitedWithNoRetryAfter_ReturnsPostponedForOneSecond()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(Answer(AcmeAnswerType.RateLimited), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(1));
    }

    [Fact]
    public void RecordSendAnswer_CircuitOpen_ReturnsPostponedUntilCircuitCloses()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);

        var result = payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: TimeSpan.FromSeconds(30)), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.SendCount.ShouldBe(0);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(30));
        payment.Changes.ShouldBeEmpty();
    }

    [Fact]
    public void RecordQueryAnswer_Approved_ReturnsApproved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        var acmeInteractionId = Guid.CreateVersion7();

        var result = payment.RecordQueryAnswer(Approved(1999, time), acmeInteractionId, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.State.ShouldBe(PaymentState.Approved);
        payment.NextActionAt.ShouldBeNull();
        var change = payment.Changes.Last();
        change.FromState.ShouldBe(PaymentState.OutcomeUnknown);
        change.ToState.ShouldBe(PaymentState.Approved);
        change.Source.ShouldBe(PaymentChangeSource.Query);
        change.AcmeInteractionId.ShouldBe(acmeInteractionId);
    }

    [Fact]
    public void RecordQueryAnswer_Processing_ReturnsQueryLaterWithNoChange()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(Answer(AcmeAnswerType.Processing), null, time);

        result.ShouldBe(AcmeAnswerResult.QueryLater);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.QueryCount.ShouldBe(1);
        payment.Changes.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    [InlineData(4, 80)]
    [InlineData(5, 160)]
    [InlineData(6, 300)]
    [InlineData(7, 300)]
    public void RecordQueryAnswer_ProcessingAgain_DoublesGapToAtMostFiveMinutes(int processingCount, int gapSeconds)
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        foreach (var _ in Enumerable.Range(0, processingCount - 1))
        {
            payment.RecordQueryAnswer(Answer(AcmeAnswerType.Processing), null, time);
        }

        payment.RecordQueryAnswer(Answer(AcmeAnswerType.Processing), null, time);

        payment.NextActionAt.ShouldBe(Now.AddSeconds(gapSeconds));
    }

    [Fact]
    public void RecordQueryAnswer_NotFound_ReturnsSendAgain()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);

        result.ShouldBe(AcmeAnswerResult.SendAgain);
        payment.State.ShouldBe(PaymentState.Recorded);
        payment.NextActionAt.ShouldBe(Now);
    }

    [Fact]
    public void RecordQueryAnswer_NotFoundAfterThreeSends_ReturnsFailedNotSubmitted()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);

        result.ShouldBe(AcmeAnswerResult.FailedNotSubmitted);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.ResponseCode.ShouldBeNull();
        payment.ClientMessage.ShouldBeNull();
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryAnswer_Refused_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(Refused(), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.ResponseCode.ShouldBeNull();
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryAnswer_RateLimited_ReturnsPostponedUntilRetryAfter()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.RateLimited, retryAfter: TimeSpan.FromSeconds(2)), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(2));
    }

    [Fact]
    public void RecordQueryAnswer_CircuitOpen_ReturnsPostponedUntilCircuitCloses()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);

        var result = payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: TimeSpan.FromSeconds(30)), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.NextActionAt.ShouldBe(Now.AddSeconds(30));
    }

    [Fact]
    public void RecordQueryAnswer_NotFoundAfterFiveUnexpectedFailures_ReturnsFailedAfterUnexpectedFailures()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 5);

        var result = payment.RecordQueryAnswer(Answer(AcmeAnswerType.NotFound), null, time);

        result.ShouldBe(AcmeAnswerResult.FailedAfterUnexpectedFailures);
        payment.State.ShouldBe(PaymentState.Failed);
        payment.UnexpectedFailureCount.ShouldBe(0);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryAnswer_ApprovedAfterFiveUnexpectedFailures_ReturnsApproved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 5);

        var result = payment.RecordQueryAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public void RecordQueryAnswer_ProcessingAfterFiveUnexpectedFailures_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 5);

        var result = payment.RecordQueryAnswer(Answer(AcmeAnswerType.Processing), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryAnswer_CircuitOpenAfterFiveUnexpectedFailures_KeepsTheCountForTheQuery()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        SetUnexpectedFailureCount(payment, 5);

        var result = payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.CircuitOpen, retryAfter: TimeSpan.FromSeconds(30)), null, time);

        result.ShouldBe(AcmeAnswerResult.Postponed);
        payment.UnexpectedFailureCount.ShouldBe(5);
    }

    private static AcmeAnswer Answer(AcmeAnswerType type) => new(type);

    private static AcmeAnswer Approved(long authorisedAmount, TimeProvider time) =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "00",
            "Approved or completed successfully",
            "Approved.",
            "238699",
            authorisedAmount,
            "MR747891457",
            "MR43851259",
            time.GetUtcNow());

    private static AcmeAnswer Declined(long? authorisedAmount, TimeProvider time) =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "51",
            "Insufficient funds",
            "There was not enough money in the account.",
            authorisedAmount: authorisedAmount,
            acmeTransactionAt: time.GetUtcNow());

    private static AcmeAnswer Failure(TimeProvider time) =>
        new(
            AcmeAnswerType.Failure,
            "Acquirer",
            "91",
            "Issuer or switch inoperative",
            "The bank could not be reached. Please try again.",
            acmeTransactionAt: time.GetUtcNow());

    private static AcmeAnswer Refused() =>
        new(AcmeAnswerType.Refused, "Application", "E01", "The body could not be parsed", "We could not process that payment.");

    // the worker counts unexpected failures with ExecuteUpdateAsync, so only a stored row carries a count
    private static void SetUnexpectedFailureCount(PaymentEntity payment, int count) =>
        typeof(PaymentEntity).GetProperty(nameof(PaymentEntity.UnexpectedFailureCount))!.SetValue(payment, count);

    private PaymentEntity NewPayment(TimeProvider time) =>
        new(storedCard, subMerchant, currency, 1999, "APP-0001", time);
}
