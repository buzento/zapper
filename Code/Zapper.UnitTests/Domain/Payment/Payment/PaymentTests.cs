using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Domain.Payment.Payment;

// the test folders of these two types share their names, so the types are named here
using AcmePaymentAdvice = Zapper.Domain.Payment.AcmePaymentAdvice;
using AcmeTransactionStatus = Zapper.Domain.Payment.AcmeTransactionStatus;

public sealed class PaymentTests
{
    private const string AccountMerchantId = "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67";

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

    [Fact]
    public void GetStatus_ReversedPayment_ReturnsReversed()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Reversal", 1999, "EVT-1", time);

        var status = payment.GetStatus();

        status.ShouldBe(PaymentStatus.Reversed);
    }

    [Fact]
    public void GetStatus_PartlyRefundedPayment_ReturnsPartiallyRefunded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time);

        var status = payment.GetStatus();

        status.ShouldBe(PaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public void GetStatus_FullyRefundedPayment_ReturnsRefunded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Refund", 1999, "EVT-1", time);

        var status = payment.GetStatus();

        status.ShouldBe(PaymentStatus.Refunded);
    }

    [Fact]
    public void DecideNextCall_MadeUnresolvedByNotification_ReturnsUnresolvedAndStopsWork()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time, currencyCode: "USD");

        var result = payment.DecideNextCall(time);

        result.ShouldBe(NextCallResult.Unresolved);
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordSendAnswer_MadeUnresolvedWhileCallWasMade_ReturnsUnresolvedAndSettlesNothing()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time, currencyCode: "USD");

        var result = payment.RecordSendAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.ResponseCode.ShouldBeNull();
        payment.NextActionAt.ShouldBeNull();
    }

    [Fact]
    public void RecordQueryAnswer_MadeUnresolvedWhileCallWasMade_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time, currencyCode: "USD");

        var result = payment.RecordQueryAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public void RecordPaymentAdvice_AgreesWithApproval_ReturnsConfirmed()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var advice = Advice(payment);
        var notification = AdviceNotification(advice, time);

        var result = payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Confirmed);
        notification.Outcome.ShouldBe(NotificationOutcome.Confirmed);
        notification.PaymentId.ShouldBe(payment.Id);
        payment.State.ShouldBe(PaymentState.Approved);
        var change = payment.Changes.Last();
        (change.FromState, change.ToState, change.Source, change.NotificationId).ShouldBe(
            (PaymentState.Approved, PaymentState.Approved, PaymentChangeSource.PaymentAdvice, notification.Id));
    }

    [Fact]
    public void RecordPaymentAdvice_AgreesWithDeclineAndItsCode_ReturnsConfirmed()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Declined(null, time), null, time);
        var advice = Advice(payment, responseCode: "51");

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Confirmed);
        payment.State.ShouldBe(PaymentState.Declined);
    }

    [Fact]
    public void RecordPaymentAdvice_DeclineWithAnotherCode_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Declined(null, time), null, time);
        var advice = Advice(payment, responseCode: "05");

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public void RecordPaymentAdvice_DisagreesWithOutcome_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var advice = Advice(payment, "Failure", "91");
        var notification = AdviceNotification(advice, time);

        var result = payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Contradiction);
        notification.Outcome.ShouldBe(NotificationOutcome.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.Changes.Last().NotificationId.ShouldBe(notification.Id);
    }

    [Fact]
    public void RecordPaymentAdvice_OutcomeNotSet_ReturnsHeldAndLeavesDueTime()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        var dueAt = payment.NextActionAt;
        var advice = Advice(payment);
        var notification = AdviceNotification(advice, time);

        var result = payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Held);
        notification.Outcome.ShouldBe(NotificationOutcome.Held);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.NextActionAt.ShouldBe(dueAt);
    }

    [Fact]
    public void RecordPaymentAdvice_AuthorisationAdvice_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var changeCount = payment.Changes.Count;
        var advice = Advice(payment, requestType: "Authorize");

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Recorded);
        payment.Changes.Count.ShouldBe(changeCount);
    }

    [Fact]
    public void RecordPaymentAdvice_StatusOther_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var advice = Advice(payment, "Other");

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Recorded);
        payment.State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public void RecordPaymentAdvice_UnresolvedPayment_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = UnresolvedPayment(time);
        var advice = Advice(payment);

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Recorded);
        payment.State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public void RecordPaymentAdvice_AnotherMerchantAccount_ReturnsUnmatched()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var advice = Advice(payment, merchantId: "5D2B0B4E-0B7C-4C55-9C8A-3D1F2A7E9B10");
        var notification = AdviceNotification(advice, time);

        var result = payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Unmatched);
        notification.Outcome.ShouldBe(NotificationOutcome.Unmatched);
        notification.PaymentId.ShouldBeNull();
        payment.State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public void RecordPaymentAdvice_SameIdentityHeldWithOtherFields_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var first = Advice(payment);
        var firstNotification = AdviceNotification(first, time);
        payment.RecordPaymentAdvice(firstNotification, first, [], AccountMerchantId, time);
        var second = Advice(payment, responseCode: "05");

        var result = payment.RecordPaymentAdvice(
            AdviceNotification(second, time), second, [firstNotification], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public void RecordPaymentAdvice_DifferentAmount_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var advice = Advice(payment, amount: 2500);

        var result = payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        result.ShouldBe(RecordPaymentAdviceResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public void RecordPaymentAdvice_ContradictionBeforeOutcome_LeavesDueTime()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        var dueAt = payment.NextActionAt;
        var advice = Advice(payment, amount: 2500);

        payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.NextActionAt.ShouldBe(dueAt);
    }

    [Fact]
    public void RecordTransactionStatus_ReversalOfApprovedPayment_ReturnsApplied()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var status = TransactionStatus(payment, "Reversal", 1999, "EVT-1");
        var notification = StatusNotification(status, time);

        var result = payment.RecordTransactionStatus(notification, status, [], time);

        result.ShouldBe(RecordTransactionStatusResult.Applied);
        notification.Outcome.ShouldBe(NotificationOutcome.Applied);
        payment.State.ShouldBe(PaymentState.Reversed);
        payment.ReversedAmount.ShouldBe(1999);
        var applied = payment.Events.ShouldHaveSingleItem();
        (applied.Event, applied.State, applied.Amount, applied.NotificationId).ShouldBe(
            (PaymentEvent.Reversal, PaymentEventState.Applied, 1999L, notification.Id));
        applied.AppliedAt.ShouldBe(Now);
        var change = payment.Changes.Last();
        (change.FromState, change.ToState, change.Source, change.NotificationId).ShouldBe(
            (PaymentState.Approved, PaymentState.Reversed, PaymentChangeSource.TransactionStatus, notification.Id));
    }

    [Fact]
    public void RecordTransactionStatus_PartialRefund_ReturnsAppliedAndPartlyRefunds()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);

        var result = RecordEvent(payment, "Refund", 500, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Applied);
        payment.State.ShouldBe(PaymentState.PartiallyRefunded);
        payment.RefundedAmount.ShouldBe(500);
    }

    [Fact]
    public void RecordTransactionStatus_SecondPartialRefund_RecordsAChangeOfTheSameState()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time);

        RecordEvent(payment, "Refund", 700, "EVT-2", time);

        payment.RefundedAmount.ShouldBe(1200);
        var change = payment.Changes.Last();
        (change.FromState, change.ToState).ShouldBe((PaymentState.PartiallyRefunded, PaymentState.PartiallyRefunded));
    }

    [Fact]
    public void RecordTransactionStatus_RefundsReachingAuthorisedAmount_MakeItRefunded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time);

        var result = RecordEvent(payment, "Refund", 1499, "EVT-2", time);

        result.ShouldBe(RecordTransactionStatusResult.Applied);
        payment.State.ShouldBe(PaymentState.Refunded);
        payment.RefundedAmount.ShouldBe(1999);
    }

    [Fact]
    public void RecordTransactionStatus_RefundPassingAuthorisedAmount_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Approved(1500, time), null, time);

        var result = RecordEvent(payment, "Refund", 1501, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.RefundedAmount.ShouldBe(0);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Contradiction);
    }

    [Fact]
    public void RecordTransactionStatus_SecondReversal_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Reversal", 1999, "EVT-1", time);

        var result = RecordEvent(payment, "Reversal", 1999, "EVT-2", time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.ReversedAmount.ShouldBe(1999);
    }

    [Fact]
    public void RecordTransactionStatus_ReversalAfterRefund_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time);

        var result = RecordEvent(payment, "Reversal", 1999, "EVT-2", time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.ReversedAmount.ShouldBe(0);
        payment.RefundedAmount.ShouldBe(500);
    }

    [Fact]
    public void RecordTransactionStatus_ReversalForAnotherAmount_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);

        var result = RecordEvent(payment, "Reversal", 1000, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.ReversedAmount.ShouldBe(0);
    }

    [Fact]
    public void RecordTransactionStatus_OutcomeNotSet_ReturnsHeldAndLeavesDueTime()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        var dueAt = payment.NextActionAt;

        var result = RecordEvent(payment, "Reversal", 1999, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Held);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Held);
        payment.State.ShouldBe(PaymentState.OutcomeUnknown);
        payment.NextActionAt.ShouldBe(dueAt);
    }

    [Fact]
    public void RecordTransactionStatus_NotAppliedBySwitch_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);

        var result = RecordEvent(payment, "Reversal", 1999, "EVT-1", time, isApproved: false, transactionResultCode: "05");

        result.ShouldBe(RecordTransactionStatusResult.Recorded);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.NotApplied);
        payment.State.ShouldBe(PaymentState.Approved);
        payment.ReversedAmount.ShouldBe(0);
    }

    [Fact]
    public void RecordTransactionStatus_UnresolvedPayment_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = UnresolvedPayment(time);

        var result = RecordEvent(payment, "Refund", 500, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Recorded);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.NotApplied);
        payment.RefundedAmount.ShouldBe(0);
    }

    [Fact]
    public void RecordTransactionStatus_DeclinedPayment_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Declined(null, time), null, time);

        var result = RecordEvent(payment, "Refund", 500, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.RefundedAmount.ShouldBe(0);
    }

    [Fact]
    public void RecordTransactionStatus_AnotherCurrency_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);

        var result = RecordEvent(payment, "Refund", 500, "EVT-1", time, currencyCode: "USD");

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.RefundedAmount.ShouldBe(0);
    }

    [Fact]
    public void RecordTransactionStatus_UnknownType_ReturnsRecorded()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);

        var result = RecordEvent(payment, "Chargeback", 1999, "EVT-1", time);

        result.ShouldBe(RecordTransactionStatusResult.Recorded);
        payment.Events.ShouldBeEmpty();
        payment.State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public void RecordTransactionStatus_SameIdentityHeldWithOtherFields_ReturnsContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = ApprovedPayment(time);
        var first = TransactionStatus(payment, "Refund", 500, "EVT-1");
        var firstNotification = StatusNotification(first, time);
        payment.RecordTransactionStatus(firstNotification, first, [], time);
        var second = TransactionStatus(payment, "Refund", 700, "EVT-1");

        var result = payment.RecordTransactionStatus(StatusNotification(second, time), second, [firstNotification], time);

        result.ShouldBe(RecordTransactionStatusResult.Contradiction);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.RefundedAmount.ShouldBe(500);
    }

    [Fact]
    public void RecordSendAnswer_ApprovalWithHeldReversal_AppliesIt()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        RecordEvent(payment, "Reversal", 1999, "EVT-1", time);

        var result = payment.RecordSendAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        payment.State.ShouldBe(PaymentState.Reversed);
        payment.ReversedAmount.ShouldBe(1999);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Applied);
    }

    [Fact]
    public void RecordQueryAnswer_ApprovalWithHeldEvents_AppliesThemInTheOrderTheSwitchProcessedThem()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time, acmeProcessedAt: Now.AddMinutes(2));
        RecordEvent(payment, "Reversal", 1999, "EVT-2", time, acmeProcessedAt: Now.AddMinutes(1));

        var result = payment.RecordQueryAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.ReversedAmount.ShouldBe(1999);
        payment.RefundedAmount.ShouldBe(0);
        payment.Events.Select(paymentEvent => (paymentEvent.Event, paymentEvent.State)).ShouldBe(
            [(PaymentEvent.Refund, PaymentEventState.Contradiction), (PaymentEvent.Reversal, PaymentEventState.Applied)]);
    }

    [Fact]
    public void RecordQueryAnswer_DeclineWithHeldRefund_MakesItAContradiction()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        RecordEvent(payment, "Refund", 500, "EVT-1", time);

        var result = payment.RecordQueryAnswer(Declined(null, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.Events.ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Contradiction);
    }

    [Fact]
    public void RecordSendAnswer_ApprovalWithHeldAdvice_ConfirmsIt()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        var advice = Advice(payment);
        var notification = AdviceNotification(advice, time);
        payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);
        var interactionId = Guid.CreateVersion7();

        var result = payment.RecordSendAnswer(Approved(1999, time), interactionId, time);

        result.ShouldBe(AcmeAnswerResult.Approved);
        notification.Outcome.ShouldBe(NotificationOutcome.Held);
        var change = payment.Changes.Last();
        (change.FromState, change.ToState, change.Source, change.NotificationId, change.AcmeInteractionId).ShouldBe(
            (PaymentState.Approved, PaymentState.Approved, PaymentChangeSource.PaymentAdvice, notification.Id, interactionId));
    }

    [Fact]
    public void RecordQueryAnswer_ApprovalWithContradictingHeldAdvice_ReturnsUnresolved()
    {
        var time = new FakeTimeProvider(Now);
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        var advice = Advice(payment, responseCode: "51");
        payment.RecordPaymentAdvice(AdviceNotification(advice, time), advice, [], AccountMerchantId, time);

        var result = payment.RecordQueryAnswer(Approved(1999, time), null, time);

        result.ShouldBe(AcmeAnswerResult.Unresolved);
        payment.State.ShouldBe(PaymentState.Unresolved);
        payment.ResponseCode.ShouldBe("00");
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

    private PaymentEntity ApprovedPayment(TimeProvider time)
    {
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Approved(1999, time), null, time);

        return payment;
    }

    // a send whose outcome is unknown, then a query Acme refuses
    private PaymentEntity UnresolvedPayment(TimeProvider time)
    {
        var payment = NewPayment(time);
        payment.RecordSendAnswer(Answer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(Refused(), null, time);

        return payment;
    }

    private static AcmePaymentAdvice Advice(
        PaymentEntity payment,
        string transactionStatus = "Success",
        string responseCode = "00",
        string requestType = "Payment",
        string merchantId = AccountMerchantId,
        long amount = 1999) =>
        new(
            requestType,
            merchantId,
            payment.Id.ToString(),
            Now,
            payment.OrderNumber,
            "MR747891457",
            payment.OrderNumber,
            "7543",
            amount,
            "ZAR",
            "CardNotPresent",
            transactionStatus,
            "Acquirer",
            responseCode,
            null,
            "238699",
            "REST");

    private static NotificationEntity AdviceNotification(AcmePaymentAdvice advice, TimeProvider time) =>
        new(
            NotificationSource.PaymentAdvice,
            advice.TransactionId,
            advice.RequestType,
            SHA256.HashData(Encoding.UTF8.GetBytes(advice.GetContent())),
            "{}",
            advice.TransactionStatus,
            advice.ResponseCode,
            "4bf92f3577b34da6a3ce929d0e0e4736",
            time);

    private static AcmeTransactionStatus TransactionStatus(
        PaymentEntity payment,
        string transactionType,
        long amount,
        string transactionUuid,
        string currencyCode = "ZAR",
        bool isApproved = true,
        string transactionResultCode = "00",
        DateTimeOffset? acmeProcessedAt = null) =>
        new(
            transactionUuid,
            transactionType,
            acmeProcessedAt ?? Now,
            payment.OrderNumber,
            amount,
            currencyCode,
            transactionResultCode,
            isApproved);

    private static NotificationEntity StatusNotification(AcmeTransactionStatus status, TimeProvider time) =>
        new(
            NotificationSource.TransactionStatus,
            status.TransactionUuid,
            null,
            SHA256.HashData(Encoding.UTF8.GetBytes(status.GetContent())),
            "{}",
            null,
            null,
            "4bf92f3577b34da6a3ce929d0e0e4736",
            time);

    private static RecordTransactionStatusResult RecordEvent(
        PaymentEntity payment,
        string transactionType,
        long amount,
        string transactionUuid,
        TimeProvider time,
        string currencyCode = "ZAR",
        bool isApproved = true,
        string transactionResultCode = "00",
        DateTimeOffset? acmeProcessedAt = null)
    {
        var status = TransactionStatus(
            payment, transactionType, amount, transactionUuid, currencyCode, isApproved, transactionResultCode, acmeProcessedAt);

        return payment.RecordTransactionStatus(StatusNotification(status, time), status, [], time);
    }
}
