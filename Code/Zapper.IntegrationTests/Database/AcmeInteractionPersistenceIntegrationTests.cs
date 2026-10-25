using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class AcmeInteractionPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly ZapperDbContext database;

    public AcmeInteractionPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveAcmeInteraction_NewInteraction_RoundTripsEveryColumn()
    {
        var payment = await SavePaymentAsync("0301");
        var messageId = Guid.CreateVersion7();
        var acmeInteraction = new AcmeInteractionEntity(
            payment.Id,
            payment.StoredCardId,
            AcmeOperation.Send,
            AcmeInteractionOutcome.Answered,
            messageId,
            2,
            """{"Token":"[removed]"}""",
            200,
            """{"PaymentResponse":{}}""",
            Now,
            412,
            "4bf92f3577b34da6a3ce929d0e0e4736");
        database.AcmeInteractions.Add(acmeInteraction);

        await database.SaveChangesAsync(CancellationToken.None);

        var stored = await database.AcmeInteractions.SingleAsync(
            candidate => candidate.Id == acmeInteraction.Id, TestContext.Current.CancellationToken);
        stored.PaymentId.ShouldBe(payment.Id);
        stored.StoredCardId.ShouldBe(payment.StoredCardId);
        stored.Operation.ShouldBe(AcmeOperation.Send);
        stored.Outcome.ShouldBe(AcmeInteractionOutcome.Answered);
        stored.MessageId.ShouldBe(messageId);
        stored.Attempt.ShouldBe(2);
        stored.Request.ShouldBe("""{"Token":"[removed]"}""");
        stored.HttpStatus.ShouldBe(200);
        stored.Response.ShouldBe("""{"PaymentResponse":{}}""");
        stored.StartedAt.ShouldBe(Now);
        stored.DurationMilliseconds.ShouldBe(412);
        stored.TraceId.ShouldBe("4bf92f3577b34da6a3ce929d0e0e4736");
    }

    [Fact]
    public async Task SaveAcmeInteraction_PaymentNotStored_ViolatesForeignKey()
    {
        var payment = await SavePaymentAsync("0302");
        database.AcmeInteractions.Add(NewInteraction(Guid.CreateVersion7(), payment.StoredCardId));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveAcmeInteraction_StoredCardNotStored_ViolatesForeignKey()
    {
        var payment = await SavePaymentAsync("0303");
        database.AcmeInteractions.Add(NewInteraction(payment.Id, Guid.CreateVersion7()));

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveAcmeInteraction_OperationNotListed_ViolatesForeignKey()
    {
        var payment = await SavePaymentAsync("0304");
        var acmeInteraction = NewInteraction(payment.Id, payment.StoredCardId);
        database.AcmeInteractions.Add(acmeInteraction);
        database.Entry(acmeInteraction).Property(stored => stored.Operation).CurrentValue = (AcmeOperation)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveAcmeInteraction_OutcomeNotListed_ViolatesForeignKey()
    {
        var payment = await SavePaymentAsync("0305");
        var acmeInteraction = NewInteraction(payment.Id, payment.StoredCardId);
        database.AcmeInteractions.Add(acmeInteraction);
        database.Entry(acmeInteraction).Property(stored => stored.Outcome).CurrentValue = (AcmeInteractionOutcome)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    private static AcmeInteractionEntity NewInteraction(Guid paymentId, Guid storedCardId) =>
        new(paymentId, storedCardId, AcmeOperation.Query, AcmeInteractionOutcome.TimedOut, null, 1, "{}", null, null, Now, 35000, "4bf92f3577b34da6a3ce929d0e0e4736");

    private async Task<PaymentEntity> SavePaymentAsync(string number)
    {
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, $"APP-{number}", new FakeTimeProvider(Now));
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);

        return payment;
    }
}
