using Zapper.Domain.Payment;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Domain.Payment.CurrencyType;

public sealed class CurrencyTypeTests
{
    [Fact]
    public void CanRecordPayment_AmountAtMaximum_ReturnsAllowed()
    {
        var currency = new CurrencyTypeEntity(Currency.Zar, "ZAR", "R", 100000000);

        var result = currency.CanRecordPayment(100000000);

        result.ShouldBe(RecordPaymentResult.Allowed);
    }

    [Fact]
    public void CanRecordPayment_AmountOneAboveMaximum_ReturnsAmountAboveMaximum()
    {
        var currency = new CurrencyTypeEntity(Currency.Zar, "ZAR", "R", 100000000);

        var result = currency.CanRecordPayment(100000001);

        result.ShouldBe(RecordPaymentResult.AmountAboveMaximum);
    }
}
