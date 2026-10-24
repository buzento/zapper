using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class PaymentChangePersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly ZapperDbContext database;

    public PaymentChangePersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SavePaymentChange_PaymentTimedOut_RoundTripsEveryColumn()
    {
        var (payment, acmeInteraction) = await SavePaymentAndInteractionAsync("0401");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), acmeInteraction.Id, new FakeTimeProvider(Now));

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.PaymentChanges.SingleAsync(
            candidate => candidate.PaymentId == payment.Id, TestContext.Current.CancellationToken);
        stored.Id.ShouldBe(payment.Changes.Single().Id);
        stored.FromState.ShouldBe(PaymentState.Recorded);
        stored.ToState.ShouldBe(PaymentState.OutcomeUnknown);
        stored.Source.ShouldBe(PaymentChangeSource.Send);
        stored.AcmeInteractionId.ShouldBe(acmeInteraction.Id);
        stored.ChangedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task SavePaymentChange_PaymentNotStored_ViolatesForeignKey()
    {
        var (savedPayment, _) = await SavePaymentAndInteractionAsync("0402");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var storedCard = await database.StoredCards.SingleAsync(
            candidate => candidate.Id == savedPayment.StoredCardId, TestContext.Current.CancellationToken);
        var subMerchant = await database.SubMerchants.SingleAsync(
            candidate => candidate.Id == savedPayment.SubMerchantId, TestContext.Current.CancellationToken);
        var unsavedPayment = new PaymentEntity(storedCard, subMerchant, currency, 1999, "APP-0402-UNSAVED", new FakeTimeProvider(Now));
        unsavedPayment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, new FakeTimeProvider(Now));
        database.PaymentChanges.Add(unsavedPayment.Changes.Single());

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentChange_FromStateNotListed_ViolatesForeignKey()
    {
        var (payment, acmeInteraction) = await SavePaymentAndInteractionAsync("0403");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), acmeInteraction.Id, new FakeTimeProvider(Now));
        database.Entry(payment.Changes.Single()).Property(change => change.FromState).CurrentValue = (PaymentState)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentChange_ToStateNotListed_ViolatesForeignKey()
    {
        var (payment, acmeInteraction) = await SavePaymentAndInteractionAsync("0404");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), acmeInteraction.Id, new FakeTimeProvider(Now));
        database.Entry(payment.Changes.Single()).Property(change => change.ToState).CurrentValue = (PaymentState)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentChange_SourceNotListed_ViolatesForeignKey()
    {
        var (payment, acmeInteraction) = await SavePaymentAndInteractionAsync("0405");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), acmeInteraction.Id, new FakeTimeProvider(Now));
        database.Entry(payment.Changes.Single()).Property(change => change.Source).CurrentValue = (PaymentChangeSource)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentChange_AcmeInteractionNotStored_ViolatesForeignKey()
    {
        var (payment, _) = await SavePaymentAndInteractionAsync("0406");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), Guid.CreateVersion7(), new FakeTimeProvider(Now));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentChange_NotificationNotStored_ViolatesForeignKey()
    {
        var (payment, acmeInteraction) = await SavePaymentAndInteractionAsync("0407");
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), acmeInteraction.Id, new FakeTimeProvider(Now));
        database.Entry(payment.Changes.Single()).Property(change => change.NotificationId).CurrentValue = Guid.CreateVersion7();

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    // the payment stays tracked, as the worker's is, so the change its domain method adds is saved with it
    private async Task<(PaymentEntity Payment, AcmeInteractionEntity AcmeInteraction)> SavePaymentAndInteractionAsync(
        string number)
    {
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, $"APP-{number}", new FakeTimeProvider(Now));
        var acmeInteraction = new AcmeInteractionEntity(
            payment.Id, storedCard.Id, AcmeOperation.Send, AcmeInteractionOutcome.TimedOut, Guid.CreateVersion7(), 1, "{}", null, null, Now, 35000, "4bf92f3577b34da6a3ce929d0e0e4736");
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        database.AcmeInteractions.Add(acmeInteraction);
        await database.SaveChangesAsync(CancellationToken.None);

        return (payment, acmeInteraction);
    }
}
