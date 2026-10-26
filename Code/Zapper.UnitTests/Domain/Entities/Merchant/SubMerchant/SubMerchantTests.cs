using Zapper.Domain.Entities.Merchant;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Domain.Entities.Merchant.SubMerchant;

public sealed class SubMerchantTests
{
    [Theory]
    [InlineData("Cafe Del Sol", "Cape Town", "WC", "ZA", "ZAP*CAFE DEL SOL       CAPE TOWN    WCZA")]
    [InlineData("cafe del sol", "cape town", "wc", "za", "ZAP*CAFE DEL SOL       CAPE TOWN    WCZA")]
    [InlineData("The Long Name Coffee Roasters", "Johannesburg North", "Gauteng", "ZA", "ZAP*THE LONG NAME COFFEJOHANNESBURG GAZA")]
    [InlineData("Café & Co.", "St. John's", "WC", "ZA", "ZAP*CAF  CO            ST JOHNS     WCZA")]
    public void GetCardAcceptorName_SubMerchant_FollowsFnbLayout(
        string tradingName,
        string city,
        string region,
        string countryCode,
        string expected)
    {
        var subMerchant = new SubMerchantEntity("ZAP0000001", tradingName, city, region, countryCode, "5812");

        var result = subMerchant.GetCardAcceptorName();

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetCardAcceptorName_AnySubMerchant_IsFortyCharacters()
    {
        var subMerchant = new SubMerchantEntity("ZAP0000001", "A", "B", "C", "D", "5812");

        var result = subMerchant.GetCardAcceptorName();

        result.Length.ShouldBe(40);
    }
}
