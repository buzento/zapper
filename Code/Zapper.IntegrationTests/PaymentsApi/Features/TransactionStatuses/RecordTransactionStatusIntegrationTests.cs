using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Zapper.ApiCommon.Errors;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.PaymentsApi.Features.TransactionStatuses;

public sealed class RecordTransactionStatusIntegrationTests : IClassFixture<PaymentsApiFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly PaymentsApiFixture api;

    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    public RecordTransactionStatusIntegrationTests(PaymentsApiFixture api)
    {
        this.api = api;
        client = api.CreateClient();
        database = api.Database;

        // the class shares one context, so what one test left tracked never reaches the next
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task RecordTransactionStatus_ReversalOfApprovedPayment_AppliesItAndStoresBodyAsReceived()
    {
        var payment = await SaveApprovedPaymentAsync("0501");
        var body = TransactionStatus(payment.OrderNumber, "Reversal", 1999, "ZAR").ToJsonString().Replace(",", ", ", StringComparison.Ordinal);

        var response = await SendAsync(body, Sign(body, api.WebhookSecret));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        var notification = (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem();
        notification.Outcome.ShouldBe(NotificationOutcome.Applied);
        notification.Body.ShouldBe(body);
        notification.RequestType.ShouldBeNull();
        notification.TraceId.Length.ShouldBe(32);
        var paymentEvent = (await GetEventsAsync(payment.Id)).ShouldHaveSingleItem();
        paymentEvent.Event.ShouldBe(PaymentEvent.Reversal);
        paymentEvent.State.ShouldBe(PaymentEventState.Applied);
        paymentEvent.AcmeProcessedAt.ShouldBe(new DateTimeOffset(2026, 10, 24, 8, 30, 0, TimeSpan.Zero));
        var stored = await GetPaymentAsync(payment.Id);
        stored.State.ShouldBe(PaymentState.Reversed);
        stored.ReversedAmount.ShouldBe(1999);
        (await GetChangesAsync(payment.Id)).ShouldHaveSingleItem().NotificationId.ShouldBe(notification.Id);
    }

    [Fact]
    public async Task RecordTransactionStatus_PartialRefund_AppliesIt()
    {
        var payment = await SaveApprovedPaymentAsync("0502");

        var response = await SendAsync(TransactionStatus(payment.OrderNumber, "Refund", 500, "ZAR"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = await GetPaymentAsync(payment.Id);
        stored.State.ShouldBe(PaymentState.PartiallyRefunded);
        stored.RefundedAmount.ShouldBe(500);
    }

    [Fact]
    public async Task RecordTransactionStatus_PaymentWithNoOutcome_HoldsEvent()
    {
        var payment = await SavePaymentAsync("0503", approved: false);

        var response = await SendAsync(TransactionStatus(payment.OrderNumber, "Reversal", 1999, "ZAR"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Held);
        (await GetEventsAsync(payment.Id)).ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Held);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Recorded);
    }

    [Fact]
    public async Task RecordTransactionStatus_UnknownType_RecordsItAndChangesNothing()
    {
        var payment = await SaveApprovedPaymentAsync("0504");

        var response = await SendAsync(TransactionStatus(payment.OrderNumber, "Chargeback", 1999, "ZAR"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Recorded);
        (await GetEventsAsync(payment.Id)).ShouldBeEmpty();
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RecordTransactionStatus_SwitchDidNotApplyIt_RecordsEventNotApplied()
    {
        var payment = await SaveApprovedPaymentAsync("0505");
        var transactionStatus = TransactionStatus(payment.OrderNumber, "Reversal", 1999, "ZAR");
        transactionStatus["isApproved"] = false;
        transactionStatus["txResultCode"] = "05";

        var response = await SendAsync(transactionStatus);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Recorded);
        (await GetEventsAsync(payment.Id)).ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.NotApplied);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RecordTransactionStatus_RetrievalReferenceNumberNamesNoPayment_StoresItUnmatched()
    {
        var transactionStatus = TransactionStatus("MR0000000506", "Refund", 500, "ZAR");

        var response = await SendAsync(transactionStatus);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notification = (await GetNotificationsAsync(transactionStatus["transactionUUID"]!.GetValue<string>())).ShouldHaveSingleItem();
        notification.PaymentId.ShouldBeNull();
        notification.Outcome.ShouldBe(NotificationOutcome.Unmatched);
    }

    [Fact]
    public async Task RecordTransactionStatus_OtherCurrency_StoresContradictionAndPaymentNeedsPerson()
    {
        var payment = await SaveApprovedPaymentAsync("0507");

        var response = await SendAsync(TransactionStatus(payment.OrderNumber, "Refund", 500, "USD"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Contradiction);
        (await GetEventsAsync(payment.Id)).ShouldHaveSingleItem().State.ShouldBe(PaymentEventState.Contradiction);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public async Task RecordTransactionStatus_SameIdentityWithOtherAmount_StoresContradictionAndPaymentNeedsPerson()
    {
        var payment = await SaveApprovedPaymentAsync("0508");
        var transactionStatus = TransactionStatus(payment.OrderNumber, "Refund", 500, "ZAR");
        await SendAsync(transactionStatus);
        transactionStatus["transactionAmount"] = 600L;

        var response = await SendAsync(transactionStatus);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).Select(notification => notification.Outcome)
            .ShouldBe([NotificationOutcome.Applied, NotificationOutcome.Contradiction], ignoreOrder: true);
        var stored = await GetPaymentAsync(payment.Id);
        stored.State.ShouldBe(PaymentState.Unresolved);
        stored.RefundedAmount.ShouldBe(500);
    }

    [Fact]
    public async Task RecordTransactionStatus_DeliveredTwice_StoresAndAppliesOnce()
    {
        var payment = await SaveApprovedPaymentAsync("0509");
        var transactionStatus = TransactionStatus(payment.OrderNumber, "Refund", 500, "ZAR");
        await SendAsync(transactionStatus);
        transactionStatus["DeliveryCount"] = 2;

        var response = await SendAsync(transactionStatus);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id)).ShouldHaveSingleItem();
        (await GetEventsAsync(payment.Id)).ShouldHaveSingleItem();
        (await GetPaymentAsync(payment.Id)).RefundedAmount.ShouldBe(500);
    }

    [Theory]
    [InlineData("transactionUUID", null, ErrorMessageCode.TransactionUuidRequired)]
    [InlineData("transactionUUID", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.TransactionUuidTooLong)]
    [InlineData("transactionType", null, ErrorMessageCode.TransactionTypeRequired)]
    [InlineData("transactionType", "012345678901234567890", ErrorMessageCode.TransactionTypeTooLong)]
    [InlineData("transactionDateTime", null, ErrorMessageCode.TransactionDateTimeRequired)]
    [InlineData("transactionDateTime", "2026-03-04T08:02:11", ErrorMessageCode.TransactionDateTimeMalformed)]
    [InlineData("rrn", null, ErrorMessageCode.RetrievalReferenceNumberRequired)]
    [InlineData("rrn", "0123456789012", ErrorMessageCode.RetrievalReferenceNumberTooLong)]
    [InlineData("transactionAmount", null, ErrorMessageCode.AmountBelowMinimum)]
    [InlineData("currencyCode", null, ErrorMessageCode.CurrencyCodeRequired)]
    [InlineData("currencyCode", "ZARX", ErrorMessageCode.CurrencyCodeTooLong)]
    [InlineData("txResultCode", null, ErrorMessageCode.TransactionResultCodeRequired)]
    [InlineData("txResultCode", "0000", ErrorMessageCode.TransactionResultCodeTooLong)]
    [InlineData("isApproved", null, ErrorMessageCode.IsApprovedRequired)]
    public async Task RecordTransactionStatus_FieldBreaksItsRule_ReturnsValidationProblemForThatFieldAndStoresNothing(
        string field,
        string? value,
        string code)
    {
        var transactionUuid = Guid.NewGuid().ToString();
        var transactionStatus = TransactionStatus("MR0000000510", "Refund", 500, "ZAR");
        transactionStatus["transactionUUID"] = transactionUuid;
        transactionStatus.Remove(field);

        if (value is not null)
        {
            transactionStatus[field] = value;
        }

        var response = await SendAsync(transactionStatus);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!["errors"]!.AsObject();
        errors.Select(error => error.Key).ShouldBe([field]);
        var message = await database.ErrorMessages.AsNoTracking()
            .SingleAsync(candidate => candidate.Code == code, TestContext.Current.CancellationToken);
        errors[field]!.AsArray().Select(detail => detail!.GetValue<string>()).ShouldBe([message.Detail]);
        (await GetNotificationsAsync(transactionUuid)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordTransactionStatus_NoSignature_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionStatus = TransactionStatus("MR0000000511", "Refund", 500, "ZAR");

        var response = await client.PostAsync(
            "/v1/transaction-statuses", JsonContent(transactionStatus.ToJsonString()), TestContext.Current.CancellationToken);

        await ShouldBeRefusedAsync(response, transactionStatus);
    }

    [Fact]
    public async Task RecordTransactionStatus_WrongSignature_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionStatus = TransactionStatus("MR0000000512", "Refund", 500, "ZAR");
        var body = transactionStatus.ToJsonString();

        var response = await SendAsync(body, Sign(body, "another-secret"));

        await ShouldBeRefusedAsync(response, transactionStatus);
    }

    [Fact]
    public async Task RecordTransactionStatus_BodyChangedByOneByteAfterSigning_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionStatus = TransactionStatus("MR0000000513", "Refund", 500, "ZAR");
        var body = transactionStatus.ToJsonString();

        var response = await SendAsync(body.Replace("\"Refund\"", "\"refund\"", StringComparison.Ordinal), Sign(body, api.WebhookSecret));

        await ShouldBeRefusedAsync(response, transactionStatus);
    }

    [Fact]
    public async Task RecordTransactionStatus_PaymentAdviceCredentials_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionStatus = TransactionStatus("MR0000000514", "Refund", 500, "ZAR");
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transaction-statuses")
        {
            Content = JsonContent(transactionStatus.ToJsonString())
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{api.PaymentAdviceUserName}:{api.PaymentAdvicePassword}")));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await ShouldBeRefusedAsync(response, transactionStatus);
    }

    [Fact]
    public async Task RecordTransactionStatus_NotificationsSwitchedOff_ReturnsUnauthorizedAndStoresNothing()
    {
        await using var switchedOff = api.WithWebHostBuilder(SwitchNotificationsOff);
        var transactionStatus = TransactionStatus("MR0000000515", "Refund", 500, "ZAR");
        var body = transactionStatus.ToJsonString();

        var response = await SendAsync(switchedOff.CreateClient(), body, Sign(body, api.WebhookSecret));

        await ShouldBeRefusedAsync(response, transactionStatus);
    }

    [Fact]
    public async Task RecordTransactionStatus_AppendixAWorkedExample_IsAccepted()
    {
        // Appendix A's example, as the Transaction Status contract document quotes it, signed with the secret the
        // brief gives it
        const string body =
            "{\"transactionUUID\":\"7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a7\",\"transactionType\":\"Reversal\","
            + "\"transactionDateTime\":\"2026-03-04T08:02:11.417\",\"rrn\":\"MR43851259\",\"transactionAmount\":1999,"
            + "\"currencyCode\":\"ZAR\",\"txResultCode\":\"00\",\"isApproved\":true}";
        await using var example = api.WithWebHostBuilder(UseWorkedExampleSecret);

        var response = await SendAsync(example.CreateClient(), body, "KURAJlPrV3BWvWzsGBgr0EzClUSfu3itG62dM5Lqy/Y=");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync("7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a7")).ShouldNotBeEmpty();
    }

    private static void SwitchNotificationsOff(IWebHostBuilder builder) =>
        builder.UseSetting("Zapper:AcmeNotification:IsEnabled", "false");

    private static void UseWorkedExampleSecret(IWebHostBuilder builder) =>
        builder.UseSetting("Zapper:AcmeNotification:WebhookSecret", "k7Qf2mZr9XpL4vNcA8sTgH3wYbE6dJ5u");

    private async Task ShouldBeRefusedAsync(HttpResponseMessage response, JsonObject transactionStatus)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["code"]!.GetValue<string>().ShouldBe(ErrorMessageCode.Unauthorized);
        (await GetNotificationsAsync(transactionStatus["transactionUUID"]!.GetValue<string>())).ShouldBeEmpty();
    }

    // a Transaction Status as Appendix A shapes it, with its own identity
    private static JsonObject TransactionStatus(
        string retrievalReferenceNumber,
        string transactionType,
        long amount,
        string currencyCode) =>
        new()
        {
            ["transactionUUID"] = Guid.NewGuid().ToString(),
            ["transactionType"] = transactionType,
            ["transactionDateTime"] = "2026-10-24T10:30:00.000",
            ["rrn"] = retrievalReferenceNumber,
            ["transactionAmount"] = amount,
            ["currencyCode"] = currencyCode,
            ["txResultCode"] = "00",
            ["isApproved"] = true
        };

    private static string Sign(string body, string secret) =>
        Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

    // the bytes are sent exactly as they were signed
    private static ByteArrayContent JsonContent(string body)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return content;
    }

    private Task<HttpResponseMessage> SendAsync(JsonObject transactionStatus)
    {
        var body = transactionStatus.ToJsonString();

        return SendAsync(body, Sign(body, api.WebhookSecret));
    }

    private Task<HttpResponseMessage> SendAsync(string body, string signature) => SendAsync(client, body, signature);

    // Acme Switch proves itself with the signature of the exact body it sends
    private static Task<HttpResponseMessage> SendAsync(HttpClient sender, string body, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transaction-statuses") { Content = JsonContent(body) };
        request.Headers.Add("x-signature", signature);

        return sender.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<List<NotificationEntity>> GetNotificationsAsync(Guid paymentId) =>
        database.Notifications.AsNoTracking()
            .Where(notification => notification.Source == NotificationSource.TransactionStatus
                                   && notification.PaymentId == paymentId)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<NotificationEntity>> GetNotificationsAsync(string transactionUuid) =>
        database.Notifications.AsNoTracking()
            .Where(notification => notification.Source == NotificationSource.TransactionStatus
                                   && notification.AcmeReference == transactionUuid)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<PaymentEventEntity>> GetEventsAsync(Guid paymentId) =>
        database.PaymentEvents.AsNoTracking()
            .Where(paymentEvent => paymentEvent.PaymentId == paymentId)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<PaymentChangeEntity>> GetChangesAsync(Guid paymentId) =>
        database.PaymentChanges.AsNoTracking()
            .Where(change => change.PaymentId == paymentId && change.Source == PaymentChangeSource.TransactionStatus)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<PaymentEntity> GetPaymentAsync(Guid paymentId) =>
        database.Payments.AsNoTracking()
            .SingleAsync(payment => payment.Id == paymentId, TestContext.Current.CancellationToken);

    private Task<PaymentEntity> SaveApprovedPaymentAsync(string number) => SavePaymentAsync(number, approved: true);

    private async Task<PaymentEntity> SavePaymentAsync(string number, bool approved)
    {
        var time = new FakeTimeProvider(Now);
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == Currency.Zar, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, 1999, $"APP-{number}", time);

        if (approved)
        {
            payment.RecordSendAnswer(
                new AcmeAnswer(AcmeAnswerType.Success, "Acquirer", "00", "Approved", "Approved.", "238699", 1999, acmeTransactionAt: Now),
                null,
                time);
        }

        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();

        return payment;
    }
}
