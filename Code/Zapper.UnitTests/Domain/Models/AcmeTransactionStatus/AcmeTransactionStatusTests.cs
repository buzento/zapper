using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Domain.Models.AcmeTransactionStatus;

public sealed class AcmeTransactionStatusTests
{
    private static readonly Zapper.Domain.Models.AcmeTransactionStatus Base = new(
            "7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a7",
            "Reversal",
            new DateTimeOffset(2026, 3, 4, 6, 2, 11, 417, TimeSpan.Zero),
            "MR43851259",
            1999L,
            "ZAR",
            "00",
            true);

    // each notification differs from Base in the one mapped field it is named for
    public static TheoryData<string, Zapper.Domain.Models.AcmeTransactionStatus> ChangedFields => new()
    {
            { "TransactionUuid", new(
                "7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a8",
                Base.TransactionType,
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                Base.Amount,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "TransactionType", new(
                Base.TransactionUuid,
                "Refund",
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                Base.Amount,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "AcmeProcessedAt", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt.AddMilliseconds(1),
                Base.RetrievalReferenceNumber,
                Base.Amount,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "RetrievalReferenceNumber", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt,
                "MR43851260",
                Base.Amount,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "Amount", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                1000L,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "CurrencyCode", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                Base.Amount,
                "USD",
                Base.TransactionResultCode,
                Base.IsApproved) },
            { "TransactionResultCode", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                Base.Amount,
                Base.CurrencyCode,
                "05",
                Base.IsApproved) },
            { "IsApproved", new(
                Base.TransactionUuid,
                Base.TransactionType,
                Base.AcmeProcessedAt,
                Base.RetrievalReferenceNumber,
                Base.Amount,
                Base.CurrencyCode,
                Base.TransactionResultCode,
                false) }
    };

    [Fact]
    public void GetContent_SameFields_ReturnsTheSameText()
    {
        var same = new Zapper.Domain.Models.AcmeTransactionStatus(
            Base.TransactionUuid,
            Base.TransactionType,
            Base.AcmeProcessedAt,
            Base.RetrievalReferenceNumber,
            Base.Amount,
            Base.CurrencyCode,
            Base.TransactionResultCode,
            Base.IsApproved);

        var content = same.GetContent();

        content.ShouldBe(Base.GetContent());
    }

    [Theory]
    [MemberData(nameof(ChangedFields))]
    public void GetContent_OneMappedFieldChanged_ReturnsOtherText(string field, Zapper.Domain.Models.AcmeTransactionStatus changed)
    {
        var content = changed.GetContent();

        content.ShouldNotBe(Base.GetContent(), field);
    }
}
