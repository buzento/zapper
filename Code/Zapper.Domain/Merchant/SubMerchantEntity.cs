namespace Zapper.Domain.Merchant;

public sealed class SubMerchantEntity
{
    // FNB's layout for a payment facilitator: the facilitator's prefix, then an asterisk, then the sub-merchant
    private const string ZapperPrefix = "ZAP";

    private const int TradingNameLength = 23 - 4;

    private const int CityLength = 13;

    private const int RegionLength = 2;

    private const int CountryCodeLength = 2;

    public SubMerchantEntity(
        string merchantReference,
        string tradingName,
        string city,
        string region,
        string countryCode,
        string merchantCategoryCode)
    {
        Id = Guid.CreateVersion7();
        MerchantReference = merchantReference;
        TradingName = tradingName;
        City = city;
        Region = region;
        CountryCode = countryCode;
        MerchantCategoryCode = merchantCategoryCode;
    }

    public Guid Id { get; private set; }

    public string MerchantReference { get; private set; }

    public string TradingName { get; private set; }

    public string City { get; private set; }

    public string Region { get; private set; }

    public string CountryCode { get; private set; }

    public string MerchantCategoryCode { get; private set; }

    public string GetCardAcceptorName() =>
        $"{ZapperPrefix}*{GetStatementPart(TradingName, TradingNameLength)}"
        + GetStatementPart(City, CityLength)
        + GetStatementPart(Region, RegionLength)
        + GetStatementPart(CountryCode, CountryCodeLength);

    private static string GetStatementPart(string value, int length)
    {
        var part = new string([.. value.ToUpperInvariant().Where(IsStatementCharacter)]);

        return part.Length > length ? part[..length] : part.PadRight(length);
    }

    private static bool IsStatementCharacter(char character) =>
        character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or ' ';
}
