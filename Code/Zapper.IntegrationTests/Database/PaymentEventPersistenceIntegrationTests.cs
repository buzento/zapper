using System.Security.Cryptography;
using System.Text;
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

public sealed class PaymentEventPersistenceIntegrationTests : IClassFixture<ClonedDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly ZapperDbContext database;

    public PaymentEventPersistenceIntegrationTests(ClonedDatabaseFixture clonedDatabase)
    {
        database = clonedDatabase.Database;

        // the class shares one context, so what a refused save left tracked never reaches the next test
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SavePaymentEvent_AppliedRefund_RoundTripsEveryColumn()
    {
        var payment = await SaveApprovedPaymentAsync("0601");
        var notification = RecordRefund(payment, "0601-1");

        await database.SaveChangesAsync(CancellationToken.None);

        database.ChangeTracker.Clear();
        var stored = await database.PaymentEvents.SingleAsync(
            candidate => candidate.PaymentId == payment.Id, TestContext.Current.CancellationToken);
        stored.NotificationId.ShouldBe(notification.Id);
        stored.Event.ShouldBe(PaymentEvent.Refund);
        stored.Amount.ShouldBe(500);
        stored.CurrencyCode.ShouldBe("ZAR");
        stored.AcmeProcessedAt.ShouldBe(Now);
        stored.State.ShouldBe(PaymentEventState.Applied);
        stored.AppliedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task SavePaymentEvent_SecondEventOfOneNotification_ReturnsAlreadyExists()
    {
        var payment = await SaveApprovedPaymentAsync("0602");
        var first = RecordRefund(payment, "0602-1");
        await database.SaveChangesAsync(CancellationToken.None);
        RecordRefund(payment, "0602-2");
        var second = payment.Events.Single(candidate => candidate.NotificationId != first.Id);
        database.Entry(second).Property(candidate => candidate.NotificationId).CurrentValue = first.Id;

        var outcome = await database.SaveChangesWithOutcomeAsync(CancellationToken.None);

        outcome.ShouldBe(SaveOutcome.AlreadyExists);
    }

    [Fact]
    public async Task SavePaymentEvent_NotificationNotStored_ViolatesForeignKey()
    {
        var payment = await SaveApprovedPaymentAsync("0603");
        var notification = RecordRefund(payment, "0603-1");
        database.Entry(notification).State = EntityState.Detached;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentEvent_PaymentNotStored_ViolatesForeignKey()
    {
        var savedPayment = await SaveApprovedPaymentAsync("0604");
        var unsavedPayment = await ApprovedPaymentAsync("0604-UNSAVED", savedPayment);
        var notification = RecordRefund(unsavedPayment, "0604-1");
        database.Entry(notification).Property(candidate => candidate.PaymentId).CurrentValue = savedPayment.Id;
        database.PaymentEvents.Add(unsavedPayment.Events.Single());

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentEvent_EventNotListed_ViolatesForeignKey()
    {
        var payment = await SaveApprovedPaymentAsync("0605");
        RecordRefund(payment, "0605-1");
        database.Entry(payment.Events.Single()).Property(candidate => candidate.Event).CurrentValue = (PaymentEvent)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SavePaymentEvent_StateNotListed_ViolatesForeignKey()
    {
        var payment = await SaveApprovedPaymentAsync("0606");
        RecordRefund(payment, "0606-1");
        database.Entry(payment.Events.Single()).Property(candidate => candidate.State).CurrentValue = (PaymentEventState)99;

        var save = async () => await database.SaveChangesAsync(CancellationToken.None);

        await save.ShouldThrowAsync<DbUpdateException>();
    }

    // the notification is added as the handler adds it, and the event its domain method makes is saved through the
    // payment
    private NotificationEntity RecordRefund(PaymentEntity payment, string transactionUuid)
    {
        var time = new FakeTimeProvider(Now);
        var status = new AcmeTransactionStatus(transactionUuid, "Refund", Now, payment.OrderNumber, 500, "ZAR", "00", true);
        var notification = new NotificationEntity(
            NotificationSource.TransactionStatus,
            status.TransactionUuid,
            null,
            SHA256.HashData(Encoding.UTF8.GetBytes(status.GetContent())),
            "{}",
            null,
            null,
            "4bf92f3577b34da6a3ce929d0e0e4736",
            time);
        payment.RecordTransactionStatus(notification, status, [], time);
        database.Notifications.Add(notification);

        return notification;
    }

    // a payment that is never added to the context, at the stored card and sub-merchant of one that was
    private async Task<PaymentEntity> ApprovedPaymentAsync(string appPaymentReference, PaymentEntity savedPayment)
    {
        var time = new FakeTimeProvider(Now);
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var storedCard = await database.StoredCards.SingleAsync(
            candidate => candidate.Id == savedPayment.StoredCardId, TestContext.Current.CancellationToken);
        var subMerchant = await database.SubMerchants.SingleAsync(
            candidate => candidate.Id == savedPayment.SubMerchantId, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, appPaymentReference, time);
        payment.RecordSendAnswer(
            new AcmeAnswer(AcmeAnswerType.Success, "Acquirer", "00", "Approved", "Approved.", "238699", 1999, acmeTransactionAt: Now),
            null,
            time);

        return payment;
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
