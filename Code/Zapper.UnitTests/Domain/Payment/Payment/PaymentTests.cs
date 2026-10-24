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
}
