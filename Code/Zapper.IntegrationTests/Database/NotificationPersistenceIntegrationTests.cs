using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Database;

public sealed class NotificationPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private const string AccountMerchantId = "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67";

    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly ZapperDbContext database;

    public NotificationPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveNotification_ConfirmingPaymentAdvice_RoundTripsEveryColumn()
    {
        var payment = await SaveApprovedPaymentAsync("0501");
        var notification = RecordPaymentAdvice(payment, "00");
        database.Notifications.Add(notification);

        await database.SaveChangesAsync(CancellationToken.None);

        database.ChangeTracker.Clear();
        var stored = await database.Notifications.SingleAsync(
            candidate => candidate.Id == notification.Id, TestContext.Current.CancellationToken);
        stored.Source.ShouldBe(NotificationSource.PaymentAdvice);
        stored.AcmeReference.ShouldBe(payment.Id.ToString());
        stored.RequestType.ShouldBe("Payment");
        stored.ContentHash.ShouldBe(notification.ContentHash);
        stored.PaymentId.ShouldBe(payment.Id);
        stored.Outcome.ShouldBe(NotificationOutcome.Confirmed);
        stored.TransactionStatus.ShouldBe("Success");
        stored.ResponseCode.ShouldBe("00");
        stored.Body.ShouldBe("""{"ResponseCode":"00"}""");
        stored.ReceivedAt.ShouldBe(Now);
        stored.TraceId.ShouldBe(TraceId);
    }

    [Fact]
    public async Task SaveNotification_SameIdentityAndContent_ReturnsAlreadyExists()
    {
        var payment = await SaveApprovedPaymentAsync("0502");
        database.Notifications.Add(RecordPaymentAdvice(payment, "00"));
        await database.SaveChangesAsync(CancellationToken.None);
        database.Notifications.Add(RecordPaymentAdvice(payment, "00"));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }

    [Fact]
    public async Task SaveNotification_SameIdentityOtherContent_IsSaved()
    {
        var payment = await SaveApprovedPaymentAsync("0503");
        database.Notifications.Add(RecordPaymentAdvice(payment, "00"));
        await database.SaveChangesAsync(CancellationToken.None);
        database.Notifications.Add(RecordPaymentAdvice(payment, "05"));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.Saved);
    }

    [Fact]
    public async Task SaveNotification_TwoTransactionStatusesWithNoRequestType_ReturnsAlreadyExists()
    {
        database.Notifications.Add(UnmatchedTransactionStatus("0504"));
        await database.SaveChangesAsync(CancellationToken.None);
        database.Notifications.Add(UnmatchedTransactionStatus("0504"));

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }

    [Fact]
    public async Task SaveNotification_SourceNotListed_ViolatesForeignKey()
    {
        var notification = UnmatchedTransactionStatus("0505");
        database.Notifications.Add(notification);
        database.Entry(notification).Property(candidate => candidate.Source).CurrentValue = (NotificationSource)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveNotification_OutcomeNotListed_ViolatesForeignKey()
    {
        var notification = UnmatchedTransactionStatus("0506");
        database.Notifications.Add(notification);
        database.Entry(notification).Property(candidate => candidate.Outcome).CurrentValue = (NotificationOutcome)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveNotification_PaymentNotStored_ViolatesForeignKey()
    {
        var notification = UnmatchedTransactionStatus("0507");
        database.Notifications.Add(notification);
        database.Entry(notification).Property(candidate => candidate.PaymentId).CurrentValue = Guid.CreateVersion7();

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    // the advice's content is its response code alone, so the same code makes the same content hash
    private static NotificationEntity RecordPaymentAdvice(PaymentEntity payment, string responseCode)
    {
        var time = new FakeTimeProvider(Now);
        var advice = new AcmePaymentAdvice(
            "Payment",
            AccountMerchantId,
            payment.Id.ToString(),
            Now,
            payment.OrderNumber,
            "MR747891457",
            payment.OrderNumber,
            "7543",
            payment.Amount,
            "ZAR",
            "CardNotPresent",
            "Success",
            "Acquirer",
            responseCode,
            null,
            "238699",
            "REST");
        var notification = new NotificationEntity(
            NotificationSource.PaymentAdvice,
            advice.TransactionId,
            advice.RequestType,
            SHA256.HashData(Encoding.UTF8.GetBytes(advice.GetContent())),
            $$"""{"ResponseCode":"{{responseCode}}"}""",
            advice.TransactionStatus,
            advice.ResponseCode,
            TraceId,
            time);
        payment.RecordPaymentAdvice(notification, advice, [], AccountMerchantId, time);

        return notification;
    }

    private static NotificationEntity UnmatchedTransactionStatus(string number)
    {
        var notification = new NotificationEntity(
            NotificationSource.TransactionStatus,
            $"EVT-{number}",
            null,
            SHA256.HashData(Encoding.UTF8.GetBytes(number)),
            "{}",
            null,
            null,
            TraceId,
            new FakeTimeProvider(Now));
        notification.RecordUnmatched();

        return notification;
    }

    // the payment stays tracked, as a notification's handler holds it, so what its domain methods add is saved with it
    private async Task<PaymentEntity> SaveApprovedPaymentAsync(string number)
    {
        var time = new FakeTimeProvider(Now);
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, $"APP-{number}", time);
        payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.Success, "Acquirer", "00", "Approved", "Approved.", "238699", 1999, acmeTransactionAt: Now),
            null,
            time);
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);

        return payment;
    }
}
