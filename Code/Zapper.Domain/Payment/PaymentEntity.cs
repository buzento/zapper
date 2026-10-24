using System.Security.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;

namespace Zapper.Domain.Payment;

public sealed class PaymentEntity
{
    private const string OrderNumberCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int OrderNumberLength = 12;

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

    public string? ResponseCode { get; private set; }

    public string? ClientMessage { get; private set; }

    public long? AuthorisedAmount { get; private set; }

    public long ReversedAmount { get; private set; }

    public long RefundedAmount { get; private set; }

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

    public PaymentStatus GetStatus() => PaymentStatus.Pending;
}
