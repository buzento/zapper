namespace Zapper.Domain.Payment;

public enum Currency
{
    Zar = 1,
    Usd = 2,
    Eur = 3,
    Gbp = 4
}

public sealed class CurrencyTypeEntity
{
    public CurrencyTypeEntity(Currency id, string code, string symbol, long maximumAmount)
    {
        Id = id;
        Name = id.ToString();
        Code = code;
        Symbol = symbol;
        MaximumAmount = maximumAmount;
    }

    public Currency Id { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public string Symbol { get; private set; }

    public long MaximumAmount { get; private set; }

    public RecordPaymentResult CanRecordPayment(long amount) =>
        amount > MaximumAmount
            ? RecordPaymentResult.AmountAboveMaximum
            : RecordPaymentResult.Allowed;
}
