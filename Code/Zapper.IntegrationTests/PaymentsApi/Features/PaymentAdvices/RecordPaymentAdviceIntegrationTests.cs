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

namespace Zapper.IntegrationTests.PaymentsApi.Features.PaymentAdvices;

public sealed class RecordPaymentAdviceIntegrationTests : IClassFixture<PaymentsApiFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 8, 30, 0, TimeSpan.Zero);

    private readonly PaymentsApiFixture api;

    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    public RecordPaymentAdviceIntegrationTests(PaymentsApiFixture api)
    {
        this.api = api;
        client = api.CreateClient();
        database = api.Database;

        // the class shares one context, so what one test left tracked never reaches the next
        database.ChangeTracker.Clear();
    }

    [Fact]
    public async Task RecordPaymentAdvice_AgreesWithApprovedPayment_StoresConfirmationAndChange()
    {
        var payment = await SaveApprovedPaymentAsync("0401");
        var advice = PaymentAdvice(payment, "Success", "00");
        advice["SettlementBatch"] = "B-17";

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        var notification = (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem();
        notification.PaymentId.ShouldBe(payment.Id);
        notification.Outcome.ShouldBe(NotificationOutcome.Confirmed);
        notification.TransactionStatus.ShouldBe("Success");
        notification.ResponseCode.ShouldBe("00");
        notification.TraceId.Length.ShouldBe(32);
        var stored = JsonNode.Parse(notification.Body)!.AsObject();
        stored.ContainsKey("MaskedCardNumber").ShouldBeFalse();
        stored["SettlementBatch"]!.GetValue<string>().ShouldBe("B-17");
        var change = (await GetChangesAsync(payment.Id, PaymentChangeSource.PaymentAdvice)).ShouldHaveSingleItem();
        change.NotificationId.ShouldBe(notification.Id);
        change.FromState.ShouldBe(PaymentState.Approved);
        change.ToState.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RecordPaymentAdvice_PaymentWithNoOutcome_HoldsNotification()
    {
        var payment = await SavePaymentAsync("0402", approved: false);

        var response = await SendAsync(PaymentAdvice(payment, "Success", "00"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Held);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Recorded);
    }

    [Theory]
    [InlineData("Authorize", "Success")]
    [InlineData("Payment", "Other")]
    [InlineData("Refund", "Success")]
    [InlineData("Payment", "Pending")]
    public async Task RecordPaymentAdvice_AdviceThatChangesNothing_RecordsIt(string requestType, string transactionStatus)
    {
        var payment = await SaveApprovedPaymentAsync($"041{requestType.Length}{transactionStatus.Length}");
        var advice = PaymentAdvice(payment, transactionStatus, "00");
        advice["RequestType"] = requestType;

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Recorded);
        (await GetChangesAsync(payment.Id, PaymentChangeSource.PaymentAdvice)).ShouldBeEmpty();
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RecordPaymentAdvice_TransactionIdNamesNoPayment_StoresItUnmatched()
    {
        var transactionId = Guid.NewGuid().ToString();

        var response = await SendAsync(PaymentAdvice(transactionId, "MR0000000403", 1999, api.AcmeMerchantId, "Success", "00"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notification = (await GetNotificationsAsync(transactionId)).ShouldHaveSingleItem();
        notification.PaymentId.ShouldBeNull();
        notification.Outcome.ShouldBe(NotificationOutcome.Unmatched);
    }

    [Fact]
    public async Task RecordPaymentAdvice_AnotherMerchantAccount_StoresItUnmatchedAndChangesNothing()
    {
        var payment = await SaveApprovedPaymentAsync("0404");
        var advice = PaymentAdvice(payment, "Success", "51");
        advice["MerchantID"] = Guid.NewGuid().ToString();

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notification = (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem();
        notification.PaymentId.ShouldBeNull();
        notification.Outcome.ShouldBe(NotificationOutcome.Unmatched);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Approved);
    }

    [Fact]
    public async Task RecordPaymentAdvice_DifferentAmount_StoresContradictionAndPaymentNeedsPerson()
    {
        var payment = await SaveApprovedPaymentAsync("0405");
        var advice = PaymentAdvice(payment, "Success", "00");
        advice["Amount"] = 2500L;

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem().Outcome.ShouldBe(NotificationOutcome.Contradiction);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public async Task RecordPaymentAdvice_SameIdentityWithOtherOutcome_StoresContradictionAndPaymentNeedsPerson()
    {
        var payment = await SaveApprovedPaymentAsync("0406");
        await SendAsync(PaymentAdvice(payment, "Success", "00"));

        var response = await SendAsync(PaymentAdvice(payment, "Success", "05"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id.ToString())).Select(notification => notification.Outcome)
            .ShouldBe([NotificationOutcome.Confirmed, NotificationOutcome.Contradiction], ignoreOrder: true);
        (await GetPaymentAsync(payment.Id)).State.ShouldBe(PaymentState.Unresolved);
    }

    [Fact]
    public async Task RecordPaymentAdvice_DeliveredTwice_StoresAndChangesOnce()
    {
        var payment = await SaveApprovedPaymentAsync("0407");
        var advice = PaymentAdvice(payment, "Success", "00");
        advice["DeliveryCount"] = 1;
        await SendAsync(advice);
        advice["DeliveryCount"] = 2;

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync(payment.Id.ToString())).ShouldHaveSingleItem();
        (await GetChangesAsync(payment.Id, PaymentChangeSource.PaymentAdvice)).ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("RequestType", null, ErrorMessageCode.RequestTypeRequired)]
    [InlineData("RequestType", "012345678901234567890", ErrorMessageCode.RequestTypeTooLong)]
    [InlineData("MerchantID", null, ErrorMessageCode.MerchantIdRequired)]
    [InlineData("MerchantID", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.MerchantIdTooLong)]
    [InlineData("TransactionID", null, ErrorMessageCode.TransactionIdRequired)]
    [InlineData("TransactionID", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.TransactionIdTooLong)]
    [InlineData("TransactionDateTime", "2026-03-01T13:56:36+02:00", ErrorMessageCode.TransactionDateTimeMalformed)]
    [InlineData("OrderNumber", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.OrderNumberTooLong)]
    [InlineData("ReconID", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.ReconciliationIdTooLong)]
    [InlineData("RetrievalReferenceNumber", null, ErrorMessageCode.RetrievalReferenceNumberRequired)]
    [InlineData("RetrievalReferenceNumber", "0123456789012", ErrorMessageCode.RetrievalReferenceNumberTooLong)]
    [InlineData("SystemsTraceAuditNumber", null, ErrorMessageCode.SystemsTraceAuditNumberRequired)]
    [InlineData("SystemsTraceAuditNumber", "0123456", ErrorMessageCode.SystemsTraceAuditNumberTooLong)]
    [InlineData("Amount", null, ErrorMessageCode.AmountBelowMinimum)]
    [InlineData("CurrencyCode", "ZARX", ErrorMessageCode.CurrencyCodeTooLong)]
    [InlineData("PaymentService", null, ErrorMessageCode.PaymentServiceRequired)]
    [InlineData("PaymentService", "0123456789012345678901234567890", ErrorMessageCode.PaymentServiceTooLong)]
    [InlineData("TransactionStatus", null, ErrorMessageCode.TransactionStatusRequired)]
    [InlineData("TransactionStatus", "012345678901234567890", ErrorMessageCode.TransactionStatusTooLong)]
    [InlineData("ResponseSource", null, ErrorMessageCode.ResponseSourceRequired)]
    [InlineData("ResponseSource", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.ResponseSourceTooLong)]
    [InlineData("ResponseCode", null, ErrorMessageCode.ResponseCodeRequired)]
    [InlineData("ResponseCode", "0000", ErrorMessageCode.ResponseCodeTooLong)]
    [InlineData("ResponseDescription", "012345678901234567890123456789012345678901234567890", ErrorMessageCode.ResponseDescriptionTooLong)]
    [InlineData("AuthCode", "0123456", ErrorMessageCode.AuthorisationCodeTooLong)]
    [InlineData("CommProtocol", null, ErrorMessageCode.CommunicationProtocolRequired)]
    [InlineData("CommProtocol", "HTTPS", ErrorMessageCode.CommunicationProtocolTooLong)]
    public async Task RecordPaymentAdvice_FieldBreaksItsRule_ReturnsValidationProblemForThatFieldAndStoresNothing(
        string field,
        string? value,
        string code)
    {
        var transactionId = Guid.NewGuid().ToString();
        var advice = PaymentAdvice(transactionId, "MR0000000410", 1999, api.AcmeMerchantId, "Success", "00");
        advice.Remove(field);

        if (value is not null)
        {
            advice[field] = value;
        }

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!["errors"]!.AsObject();
        errors.Select(error => error.Key).ShouldBe([field]);
        var message = await database.ErrorMessages.AsNoTracking()
            .SingleAsync(candidate => candidate.Code == code, TestContext.Current.CancellationToken);
        errors[field]!.AsArray().Select(detail => detail!.GetValue<string>()).ShouldBe([message.Detail]);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_WrongUserName_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionId = Guid.NewGuid().ToString();

        var response = await SendAsync(
            PaymentAdvice(transactionId, "MR0000000411", 1999, api.AcmeMerchantId, "Success", "00"),
            "someone-else",
            api.PaymentAdvicePassword);

        await ShouldBeUnauthorizedAsync(response);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_WrongPassword_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionId = Guid.NewGuid().ToString();

        var response = await SendAsync(
            PaymentAdvice(transactionId, "MR0000000412", 1999, api.AcmeMerchantId, "Success", "00"),
            api.PaymentAdviceUserName,
            "wrong-password");

        await ShouldBeUnauthorizedAsync(response);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_NoAuthorizationHeader_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionId = Guid.NewGuid().ToString();
        var body = PaymentAdvice(transactionId, "MR0000000413", 1999, api.AcmeMerchantId, "Success", "00").ToJsonString();

        var response = await client.PostAsync(
            "/v1/payment-advices", JsonContent(body), TestContext.Current.CancellationToken);

        await ShouldBeUnauthorizedAsync(response);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_TransactionStatusSignature_ReturnsUnauthorizedAndStoresNothing()
    {
        var transactionId = Guid.NewGuid().ToString();
        var body = PaymentAdvice(transactionId, "MR0000000414", 1999, api.AcmeMerchantId, "Success", "00").ToJsonString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/payment-advices") { Content = JsonContent(body) };
        request.Headers.Add(
            "x-signature",
            Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(api.WebhookSecret), Encoding.UTF8.GetBytes(body))));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await ShouldBeUnauthorizedAsync(response);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_NotificationsSwitchedOff_ReturnsUnauthorizedAndStoresNothing()
    {
        await using var switchedOff = api.WithWebHostBuilder(SwitchNotificationsOff);
        var transactionId = Guid.NewGuid().ToString();

        var response = await SendAsync(
            switchedOff.CreateClient(),
            PaymentAdvice(transactionId, "MR0000000415", 1999, api.AcmeMerchantId, "Success", "00"),
            api.PaymentAdviceUserName,
            api.PaymentAdvicePassword);

        await ShouldBeUnauthorizedAsync(response);
        (await GetNotificationsAsync(transactionId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordPaymentAdvice_AppendixAWorkedExample_IsAccepted()
    {
        // Appendix A's example, as the Payment Advice contract document quotes it; it names another account
        var advice = new JsonObject
        {
            ["RequestType"] = "Payment",
            ["MerchantID"] = "8B67BF88-BB8D-4EB5-ADFC-0F5C97C7ED67",
            ["TransactionID"] = "2e0e1b85-bf91-45a9-b038-282e00036919",
            ["TransactionDateTime"] = "2026-03-01T13:56:36.533",
            ["OrderNumber"] = "MR4385125900",
            ["ReconID"] = "MR747891457",
            ["RetrievalReferenceNumber"] = "MR4385125900",
            ["SystemsTraceAuditNumber"] = "7543",
            ["Amount"] = 1999,
            ["CurrencyCode"] = "ZAR",
            ["PaymentService"] = "CardNotPresent",
            ["TransactionStatus"] = "Success",
            ["ResponseSource"] = "Acquirer",
            ["ResponseCode"] = "00",
            ["ResponseDescription"] = "Approved",
            ["AuthCode"] = "238699",
            ["MaskedCardNumber"] = "541333******1429",
            ["CommProtocol"] = "REST"
        };

        var response = await SendAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetNotificationsAsync("2e0e1b85-bf91-45a9-b038-282e00036919")).ShouldNotBeEmpty();
    }

    private static void SwitchNotificationsOff(IWebHostBuilder builder) =>
        builder.UseSetting("Zapper:AcmeNotification:IsEnabled", "false");

    private static async Task ShouldBeUnauthorizedAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["code"]!.GetValue<string>().ShouldBe(ErrorMessageCode.Unauthorized);
    }

    // the bytes are sent exactly as they were written
    private static ByteArrayContent JsonContent(string body)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return content;
    }

    private JsonObject PaymentAdvice(PaymentEntity payment, string transactionStatus, string responseCode) =>
        PaymentAdvice(payment.Id.ToString(), payment.OrderNumber, payment.Amount, api.AcmeMerchantId, transactionStatus, responseCode);

    // a Payment Advice as Appendix A shapes it, with every member it names
    private static JsonObject PaymentAdvice(
        string transactionId,
        string orderNumber,
        long amount,
        string merchantId,
        string transactionStatus,
        string responseCode) =>
        new()
        {
            ["RequestType"] = "Payment",
            ["MerchantID"] = merchantId,
            ["TransactionID"] = transactionId,
            ["TransactionDateTime"] = "2026-10-24T10:30:00.000",
            ["OrderNumber"] = orderNumber,
            ["ReconID"] = "MR747891457",
            ["RetrievalReferenceNumber"] = orderNumber,
            ["SystemsTraceAuditNumber"] = "7543",
            ["Amount"] = amount,
            ["CurrencyCode"] = "ZAR",
            ["PaymentService"] = "CardNotPresent",
            ["TransactionStatus"] = transactionStatus,
            ["ResponseSource"] = "Acquirer",
            ["ResponseCode"] = responseCode,
            ["ResponseDescription"] = "Approved",
            ["AuthCode"] = "238699",
            ["MaskedCardNumber"] = "541333******1429",
            ["CommProtocol"] = "REST"
        };

    private Task<HttpResponseMessage> SendAsync(JsonObject advice) =>
        SendAsync(client, advice, api.PaymentAdviceUserName, api.PaymentAdvicePassword);

    private Task<HttpResponseMessage> SendAsync(JsonObject advice, string userName, string password) =>
        SendAsync(client, advice, userName, password);

    // Acme Pay proves itself with the Basic username and password Zapper gave it
    private static Task<HttpResponseMessage> SendAsync(
        HttpClient sender,
        JsonObject advice,
        string userName,
        string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/payment-advices")
        {
            Content = JsonContent(advice.ToJsonString())
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));

        return sender.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<List<NotificationEntity>> GetNotificationsAsync(string transactionId) =>
        database.Notifications.AsNoTracking()
            .Where(notification => notification.Source == NotificationSource.PaymentAdvice
                                   && notification.AcmeReference == transactionId)
            .ToListAsync(TestContext.Current.CancellationToken);

    private Task<List<PaymentChangeEntity>> GetChangesAsync(Guid paymentId, PaymentChangeSource source) =>
        database.PaymentChanges.AsNoTracking()
            .Where(change => change.PaymentId == paymentId && change.Source == source)
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
