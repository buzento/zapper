using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Domain.Models;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class PaymentPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly ZapperDbContext database;

    public PaymentPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SavePayment_NewPayment_RoundTripsEveryColumn()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0201");
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0201", new FakeTimeProvider(Now));
        database.Payments.Add(payment);

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.Payments.SingleAsync(
            candidate => candidate.Id == payment.Id, TestContext.Current.CancellationToken);
        stored.AppPaymentReference.ShouldBe("APP-0201");
        stored.StoredCardId.ShouldBe(storedCard.Id);
        stored.SubMerchantId.ShouldBe(subMerchant.Id);
        stored.Currency.ShouldBe(Currency.Zar);
        stored.Amount.ShouldBe(1999);
        stored.OrderNumber.ShouldBe(payment.OrderNumber);
        stored.CardAcceptorName.ShouldBe("ZAP*CAFE DEL SOL       CAPE TOWN    WCZA");
        stored.MerchantCategoryCode.ShouldBe("5812");
        stored.State.ShouldBe(PaymentState.Recorded);
        stored.RecordedAt.ShouldBe(Now);
        stored.NextActionAt.ShouldBe(Now);
        stored.ResponseCode.ShouldBeNull();
        stored.ClientMessage.ShouldBeNull();
        stored.AuthorisedAmount.ShouldBeNull();
        stored.ReversedAmount.ShouldBe(0);
        stored.RefundedAmount.ShouldBe(0);
        stored.AttemptCount.ShouldBe(0);
        stored.SendCount.ShouldBe(0);
        stored.QueryCount.ShouldBe(0);
        stored.UnexpectedFailureCount.ShouldBe(0);
    }

    [Fact]
    public async Task SavePayment_Approved_RoundTripsAcmeOutcome()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0209");
        var time = new FakeTimeProvider(Now);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0209", time);
        payment.RecordSendAnswer(
            new AcmeAnswer(
                AcmeAnswerType.Success,
                "Acquirer",
                "00",
                "Approved or completed successfully",
                "Approved.",
                "238699",
                1500,
                "MR747891457",
                "MR43851259",
                Now),
            null,
            time);
        database.Payments.Add(payment);

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.Payments.SingleAsync(
            candidate => candidate.Id == payment.Id, TestContext.Current.CancellationToken);
        stored.State.ShouldBe(PaymentState.Approved);
        stored.NextActionAt.ShouldBeNull();
        stored.ResponseSource.ShouldBe("Acquirer");
        stored.ResponseCode.ShouldBe("00");
        stored.ResponseDescription.ShouldBe("Approved or completed successfully");
        stored.ClientMessage.ShouldBe("Approved.");
        stored.AuthorisationCode.ShouldBe("238699");
        stored.AuthorisedAmount.ShouldBe(1500);
        stored.ReconciliationId.ShouldBe("MR747891457");
        stored.RetrievalReferenceNumber.ShouldBe("MR43851259");
        stored.AcmeTransactionAt.ShouldBe(Now);
        stored.SendCount.ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesWithOutcome_PaymentChangedByAnotherWriter_ReturnsConflict()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0210");
        var time = new FakeTimeProvider(Now);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0210", time);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);
        await database.Payments
            .Where(candidate => candidate.Id == payment.Id)
            .ExecuteUpdateAsync(
                update => update.SetProperty(candidate => candidate.NextActionAt, Now.AddMinutes(1)),
                TestContext.Current.CancellationToken);
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, time);

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.Conflict);
    }

    [Fact]
    public async Task SaveChangesWithOutcome_AppPaymentReferenceAlreadyHeld_ReturnsAlreadyExists()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0202");
        database.Payments.Add(new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0202", new FakeTimeProvider(Now)));
        await database.SaveChangesAsync(CancellationToken.None);
        database.Payments.Add(new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0202", new FakeTimeProvider(Now)));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }

    [Fact]
    public async Task SaveChangesWithOutcome_OrderNumberAlreadyHeld_ReturnsAlreadyExists()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0203");
        var first = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0203", new FakeTimeProvider(Now));
        database.Payments.Add(first);
        await database.SaveChangesAsync(CancellationToken.None);
        var second = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0204", new FakeTimeProvider(Now));
        database.Payments.Add(second);
        database.Entry(second).Property(payment => payment.OrderNumber).CurrentValue = first.OrderNumber;

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }

    [Fact]
    public async Task SavePayment_StoredCardNotStored_ViolatesForeignKey()
    {
        var (_, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0205");
        var unsavedStoredCard = new StoredCardEntity("CUS-0205-UNSAVED", [1], "unsaved@example.test");
        database.Payments.Add(new PaymentEntity(unsavedStoredCard, subMerchant, currency, 1999, "APP-0205", new FakeTimeProvider(Now)));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePayment_SubMerchantNotStored_ViolatesForeignKey()
    {
        var (storedCard, _, currency) = await SaveCardAndSubMerchantAsync("0206");
        var unsavedSubMerchant = new SubMerchantEntity("ZAP0206UNSAVED", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        database.Payments.Add(new PaymentEntity(storedCard, unsavedSubMerchant, currency, 1999, "APP-0206", new FakeTimeProvider(Now)));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePayment_CurrencyNotListed_ViolatesForeignKey()
    {
        var (storedCard, subMerchant, _) = await SaveCardAndSubMerchantAsync("0207");
        var unlistedCurrency = new CurrencyTypeEntity((Currency)99, "XXX", "X", 1000);
        database.Payments.Add(new PaymentEntity(storedCard, subMerchant, unlistedCurrency, 1999, "APP-0207", new FakeTimeProvider(Now)));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePayment_StateNotListed_ViolatesForeignKey()
    {
        var (storedCard, subMerchant, currency) = await SaveCardAndSubMerchantAsync("0208");
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0208", new FakeTimeProvider(Now));
        database.Payments.Add(payment);
        database.Entry(payment).Property(stored => stored.State).CurrentValue = (PaymentState)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    private async Task<(StoredCardEntity StoredCard, SubMerchantEntity SubMerchant, CurrencyTypeEntity Currency)>
        SaveCardAndSubMerchantAsync(string number)
    {
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        await database.SaveChangesAsync(CancellationToken.None);
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);

        return (storedCard, subMerchant, currency);
    }
}
