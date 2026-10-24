using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Customer;
using Zapper.Domain.Merchant;
using Zapper.Domain.Payment;
using Zapper.Infrastructure.Integrations.Acme;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Acceptance;

[Collection("Acceptance")]
public sealed class AcmeNotificationsAcceptanceTests : AcceptanceTestBase
{
    private const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    // Acme writes its times in South African time, with no offset
    private static readonly TimeSpan SouthAfricanOffset = TimeSpan.FromHours(2);

    private readonly AcceptanceFixture acceptance;

    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    private readonly IAcmeService acme;

    private readonly FakeTimeProvider time;

    public AcmeNotificationsAcceptanceTests(AcceptanceFixture acceptance)
    {
        this.acceptance = acceptance;
        client = acceptance.GetClient("Zapper.PaymentsApi");
        database = acceptance.Database;
        acme = acceptance.Acme;
        time = acceptance.Time;
    }

    private static string SubMerchantReference => $"ZAP{Discriminator}";

    private static string CustomerReference => $"CUS-3001-{Discriminator}";

    private static string EmailAddress => $"cus-3001-{Discriminator}@example.test";

    private static string CardToken => $"test-token-{Discriminator}";

    // shaped as Acme's identifiers are, and carrying the discriminator, so no two tests and no two runs share one
    private static string UnknownTransactionId => $"00000000-0000-7000-8000-{Discriminator}";

    // an order number is capital letters and digits, so a lower case value never names a payment
    private static string UnknownRetrievalReferenceNumber => Discriminator;

    [Fact(DisplayName = "Payment Advice without the agreed credentials is refused")]
    [Trait("Rule", "R-1")]
    public async Task PaymentAdviceWithoutTheAgreedCredentialsIsRefused()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-1");

        var response = await SendPaymentAdviceAsync(
            await PaymentAdviceAsync(id, "Success", "00"),
            Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)));

        await ShouldBeRefusedAsUnauthorizedAsync(response);
    }

    [Fact(DisplayName = "Transaction Status whose body was changed after signing is refused")]
    [Trait("Rule", "R-1")]
    public async Task TransactionStatusWhoseBodyWasChangedAfterSigningIsRefused()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-2");
        var signed = (await TransactionStatusAsync(id, "Reversal", 1999, "ZAR")).ToJsonString();
        var changed = signed.Replace("\"transactionAmount\":1999", "\"transactionAmount\":1990", StringComparison.Ordinal);

        var response = await SendTransactionStatusAsync(changed, Sign(signed));

        await ShouldBeRefusedAsUnauthorizedAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Payment Advice that agrees with the outcome confirms it")]
    [Trait("Rule", "R-2")]
    [Trait("Rule", "R-8")]
    public async Task PaymentAdviceThatAgreesWithTheOutcomeConfirmsIt()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-3");

        var response = await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Success", "00"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Payment Advice carrying a field Acme added is accepted")]
    [Trait("Rule", "R-3")]
    public async Task PaymentAdviceCarryingAFieldAcmeAddedIsAccepted()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-4");
        var advice = await PaymentAdviceAsync(id, "Success", "00");
        advice["SettlementBatch"] = "SB-20260301-01";

        var response = await SendPaymentAdviceAsync(advice);

        await ShouldBeAnsweredWithNoBodyAsync(response);
    }

    [Fact(DisplayName = "Payment Advice missing a required field is refused")]
    [Trait("Rule", "R-3")]
    public async Task PaymentAdviceMissingARequiredFieldIsRefused()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-5");
        var advice = await PaymentAdviceAsync(id, "Success", "00");
        advice.Remove("ResponseCode");

        var response = await SendPaymentAdviceAsync(advice);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        // a 400 always carries a problem body
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["errors"]!.AsObject().Select(field => field.Key).ShouldBe(["ResponseCode"]);
    }

    [Fact(DisplayName = "Payment Advice delivered twice changes nothing the second time")]
    [Trait("Rule", "R-4")]
    public async Task PaymentAdviceDeliveredTwiceChangesNothingTheSecondTime()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-6");
        var advice = await PaymentAdviceAsync(id, "Success", "00");
        advice["DeliveryCount"] = 1;
        await ShouldBeAnsweredWithNoBodyAsync(await SendPaymentAdviceAsync(advice));
        advice["DeliveryCount"] = 2;

        var response = await SendPaymentAdviceAsync(advice);

        await ShouldBeAnsweredWithNoBodyAsync(response);
    }

    [Fact(DisplayName = "Transaction Status delivered twice is applied once")]
    [Trait("Rule", "R-4")]
    public async Task TransactionStatusDeliveredTwiceIsAppliedOnce()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-7");
        var refund = await TransactionStatusAsync(id, "Refund", 500, "ZAR");
        await ShouldBeAnsweredWithNoBodyAsync(await SendTransactionStatusAsync(refund));

        var response = await SendTransactionStatusAsync(refund);

        await ShouldBeAnsweredWithNoBodyAsync(response);
        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("PartiallyRefunded");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(500);
    }

    [Fact(DisplayName = "Same Payment Advice with a different outcome needs a person")]
    [Trait("Rule", "R-5")]
    public async Task SamePaymentAdviceWithADifferentOutcomeNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-8");
        await ShouldBeAnsweredWithNoBodyAsync(await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Success", "00")));

        var response = await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Success", "05"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Payment Advice for an unknown payment is kept")]
    [Trait("Rule", "R-6")]
    public async Task PaymentAdviceForAnUnknownPaymentIsKept()
    {
        await ArrangeBackgroundAsync();

        var response = await SendPaymentAdviceAsync(
            PaymentAdvice(UnknownTransactionId, UnknownRetrievalReferenceNumber, "Success", "00"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
    }

    [Fact(DisplayName = "Payment Advice naming another merchant account is not applied")]
    [Trait("Rule", "R-6")]
    public async Task PaymentAdviceNamingAnotherMerchantAccountIsNotApplied()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-9");
        var advice = await PaymentAdviceAsync(id, "Success", "51");
        advice["MerchantID"] = Guid.NewGuid().ToString();

        var response = await SendPaymentAdviceAsync(advice);

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Transaction Status for an unknown payment is kept")]
    [Trait("Rule", "R-6")]
    public async Task TransactionStatusForAnUnknownPaymentIsKept()
    {
        await ArrangeBackgroundAsync();

        var response = await SendTransactionStatusAsync(
            TransactionStatus(UnknownRetrievalReferenceNumber, "Refund", 500, "ZAR"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
    }

    [Fact(DisplayName = "Payment Advice for a different amount needs a person")]
    [Trait("Rule", "R-7")]
    public async Task PaymentAdviceForADifferentAmountNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-10");
        var advice = await PaymentAdviceAsync(id, "Success", "00");
        advice["Amount"] = 2500L;

        var response = await SendPaymentAdviceAsync(advice);

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Early Payment Advice is compared when the query sets the outcome")]
    [Trait("Rule", "R-8")]
    public async Task EarlyPaymentAdviceIsComparedWhenTheQuerySetsTheOutcome()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("NOT-11");
        var orderNumber = await GetOrderNumberAsync(id);
        await SendTimesOutAsync(id, QueryAnswered(Approved(orderNumber)));

        var response = await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Success", "00"));

        // the advice is the act, so it must have been taken for the outcome to be compared later
        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Pending");
        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(30));
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Early Payment Advice that the query contradicts needs a person")]
    [Trait("Rule", "R-8")]
    public async Task EarlyPaymentAdviceThatTheQueryContradictsNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("NOT-12");
        var orderNumber = await GetOrderNumberAsync(id);
        await SendTimesOutAsync(id, QueryAnswered(Declined(orderNumber)));

        var response = await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Success", "00"));
        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(30));

        // the advice is the act, so it must have been taken for the query's answer to contradict it
        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Authorisation advice is recorded and changes nothing")]
    [Trait("Rule", "R-9")]
    public async Task AuthorisationAdviceIsRecordedAndChangesNothing()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-13");
        var advice = await PaymentAdviceAsync(id, "Success", "00");
        advice["RequestType"] = "Authorize";

        var response = await SendPaymentAdviceAsync(advice);

        await ShouldBeAnsweredWithNoBodyAsync(response);
    }

    [Fact(DisplayName = "Payment Advice with status Other is recorded and changes nothing")]
    [Trait("Rule", "R-9")]
    public async Task PaymentAdviceWithStatusOtherIsRecordedAndChangesNothing()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-14");

        var response = await SendPaymentAdviceAsync(await PaymentAdviceAsync(id, "Other", "00"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    [Fact(DisplayName = "Approved payment reversed by Acme Switch shows as reversed")]
    [Trait("Rule", "R-10")]
    [Trait("Rule", "R-16")]
    public async Task ApprovedPaymentReversedByAcmeSwitchShowsAsReversed()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-15");

        var response = await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Reversed");
        payment["reversedAmount"]!.GetValue<long>().ShouldBe(1999);
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Second reversal of a payment needs a person")]
    [Trait("Rule", "R-10")]
    public async Task SecondReversalOfAPaymentNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-16");
        await ShouldBeAnsweredWithNoBodyAsync(
            await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR", eventNumber: 1)));

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR", eventNumber: 2));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["reversedAmount"]!.GetValue<long>().ShouldBe(1999);
    }

    [Fact(DisplayName = "Reversal after a refund needs a person")]
    [Trait("Rule", "R-10")]
    public async Task ReversalAfterARefundNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-17");
        await ShouldBeAnsweredWithNoBodyAsync(
            await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "ZAR", eventNumber: 1)));

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR", eventNumber: 2));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["reversedAmount"]!.GetValue<long>().ShouldBe(0);
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(500);
    }

    [Fact(DisplayName = "Partial refund shows as partially refunded")]
    [Trait("Rule", "R-11")]
    [Trait("Rule", "R-16")]
    public async Task PartialRefundShowsAsPartiallyRefunded()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-18");

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "ZAR"));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("PartiallyRefunded");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(500);
    }

    [Fact(DisplayName = "Refunds that add up to the authorised amount show as refunded")]
    [Trait("Rule", "R-11")]
    [Trait("Rule", "R-16")]
    public async Task RefundsThatAddUpToTheAuthorisedAmountShowAsRefunded()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-19");
        await ShouldBeAnsweredWithNoBodyAsync(
            await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "ZAR", eventNumber: 1)));

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 1499, "ZAR", eventNumber: 2));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Refunded");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(1999);
    }

    [Fact(DisplayName = "Refund that would pass the authorised amount needs a person")]
    [Trait("Rule", "R-11")]
    [Trait("Rule", "R-16")]
    public async Task RefundThatWouldPassTheAuthorisedAmountNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-20", authorisedAmount: 1500);

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 1999, "ZAR"));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["authorisedAmount"]!.GetValue<long>().ShouldBe(1500);
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Reversal that arrives before the outcome is applied once the payment is approved")]
    [Trait("Rule", "R-12")]
    public async Task ReversalThatArrivesBeforeTheOutcomeIsAppliedOnceThePaymentIsApproved()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("NOT-21");
        var orderNumber = await GetOrderNumberAsync(id);
        // AcmeService reads Reversed with code 00 as Success, and keeps the status as sent in the body
        await SendTimesOutAsync(
            id,
            QueryAnswered(Approved(orderNumber), """{"PaymentResponse":{"TransactionStatus":"Reversed"}}"""));

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR"));

        (await GetStatusAsync(id)).ShouldBe("Pending");
        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(30));
        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Reversed");
        payment["reversedAmount"]!.GetValue<long>().ShouldBe(1999);
    }

    [Fact(DisplayName = "Reversal of a payment that is then declined needs a person")]
    [Trait("Rule", "R-12")]
    public async Task ReversalOfAPaymentThatIsThenDeclinedNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordPaymentAsync("NOT-22");
        var orderNumber = await GetOrderNumberAsync(id);
        await SendTimesOutAsync(id, QueryAnswered(Declined(orderNumber)));

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Reversal", 1999, "ZAR"));
        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(30));

        (await GetStatusAsync(id)).ShouldBe("Unresolved");
    }

    [Fact(DisplayName = "Reversal the switch did not apply is recorded and changes nothing")]
    [Trait("Rule", "R-13")]
    public async Task ReversalTheSwitchDidNotApplyIsRecordedAndChangesNothing()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-23");
        var reversal = await TransactionStatusAsync(id, "Reversal", 1999, "ZAR");
        reversal["isApproved"] = false;
        reversal["txResultCode"] = "05";

        var response = await SendTransactionStatusAsync(reversal);

        await ShouldBeAnsweredWithNoBodyAsync(response);
        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Approved");
        payment["reversedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Refund for a declined payment needs a person")]
    [Trait("Rule", "R-13")]
    public async Task RefundForADeclinedPaymentNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordDeclinedPaymentAsync("NOT-24");

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "ZAR"));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Refund for a payment that needs a person is kept and not applied")]
    [Trait("Rule", "R-13")]
    public async Task RefundForAPaymentThatNeedsAPersonIsKeptAndNotApplied()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordUnresolvedPaymentAsync("NOT-25");

        var response = await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "ZAR"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Refund in another currency needs a person")]
    [Trait("Rule", "R-14")]
    public async Task RefundInAnotherCurrencyNeedsAPerson()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-26");

        await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Refund", 500, "USD"));

        var payment = await GetPaymentAsync(id);
        payment["status"]!.GetValue<string>().ShouldBe("Unresolved");
        payment["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Transaction Status of an unknown type is recorded and changes nothing")]
    [Trait("Rule", "R-15")]
    public async Task TransactionStatusOfAnUnknownTypeIsRecordedAndChangesNothing()
    {
        await ArrangeBackgroundAsync();
        var id = await RecordApprovedPaymentAsync("NOT-27");

        var response = await SendTransactionStatusAsync(await TransactionStatusAsync(id, "Chargeback", 1999, "ZAR"));

        await ShouldBeAnsweredWithNoBodyAsync(response);
        (await GetStatusAsync(id)).ShouldBe("Approved");
    }

    private static async Task ShouldBeAnsweredWithNoBodyAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    private static async Task ShouldBeRefusedAsUnauthorizedAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // a 401 always carries a problem body
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["code"]!.GetValue<string>().ShouldBe("Unauthorized");
    }

    private static SubmitPaymentRequest SentFor(Guid id) =>
        Arg.Is<SubmitPaymentRequest>(request => request.TransactionId == id);

    private string GetSouthAfricanTime() =>
        time.GetUtcNow().ToOffset(SouthAfricanOffset).ToString(TimeFormat, CultureInfo.InvariantCulture);

    // a Payment Advice as Appendix A shapes it, with every member it names
    private JsonObject PaymentAdvice(
        string transactionId,
        string orderNumber,
        string transactionStatus,
        string responseCode) =>
        new()
        {
            ["RequestType"] = "Payment",
            ["MerchantID"] = acceptance.AcmeMerchantId,
            ["TransactionID"] = transactionId,
            ["TransactionDateTime"] = GetSouthAfricanTime(),
            ["OrderNumber"] = orderNumber,
            ["ReconID"] = "MR747891457",
            ["RetrievalReferenceNumber"] = orderNumber,
            ["SystemsTraceAuditNumber"] = "7543",
            ["Amount"] = 1999L,
            ["CurrencyCode"] = "ZAR",
            ["PaymentService"] = "CardNotPresent",
            ["TransactionStatus"] = transactionStatus,
            ["ResponseSource"] = "Acquirer",
            ["ResponseCode"] = responseCode,
            ["ResponseDescription"] = responseCode == "00" ? "Approved" : "Not approved",
            ["AuthCode"] = "238699",
            ["MaskedCardNumber"] = "541333******1429",
            ["CommProtocol"] = "REST"
        };

    private async Task<JsonObject> PaymentAdviceAsync(Guid id, string transactionStatus, string responseCode) =>
        PaymentAdvice(id.ToString(), await GetOrderNumberAsync(id), transactionStatus, responseCode);

    // a Transaction Status as Appendix A shapes it; each event of one test has its own identity, carrying the
    // discriminator
    private JsonObject TransactionStatus(
        string retrievalReferenceNumber,
        string transactionType,
        long amount,
        string currencyCode,
        int eventNumber = 1) =>
        new()
        {
            ["transactionUUID"] = $"00000000-0000-4000-{0x8000 + eventNumber:x4}-{Discriminator}",
            ["transactionType"] = transactionType,
            ["transactionDateTime"] = GetSouthAfricanTime(),
            ["rrn"] = retrievalReferenceNumber,
            ["transactionAmount"] = amount,
            ["currencyCode"] = currencyCode,
            ["txResultCode"] = "00",
            ["isApproved"] = true
        };

    private async Task<JsonObject> TransactionStatusAsync(
        Guid id,
        string transactionType,
        long amount,
        string currencyCode,
        int eventNumber = 1) =>
        TransactionStatus(await GetOrderNumberAsync(id), transactionType, amount, currencyCode, eventNumber);

    private Task<HttpResponseMessage> SendPaymentAdviceAsync(JsonObject advice) =>
        SendPaymentAdviceAsync(advice, acceptance.PaymentAdvicePassword);

    // Acme Pay proves itself with the Basic username and password Zapper gave it
    private Task<HttpResponseMessage> SendPaymentAdviceAsync(JsonObject advice, string password)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{acceptance.PaymentAdviceUserName}:{password}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/payment-advices")
        {
            Content = JsonContent(advice.ToJsonString())
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> SendTransactionStatusAsync(JsonObject transactionStatus)
    {
        var body = transactionStatus.ToJsonString();

        return SendTransactionStatusAsync(body, Sign(body));
    }

    // Acme Switch proves itself with the signature of the exact body it sends
    private Task<HttpResponseMessage> SendTransactionStatusAsync(string body, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/transaction-statuses")
        {
            Content = JsonContent(body)
        };
        request.Headers.Add("x-signature", signature);

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private string Sign(string body) =>
        Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(acceptance.WebhookSecret),
            Encoding.UTF8.GetBytes(body)));

    // the bytes are sent exactly as they were signed
    private static ByteArrayContent JsonContent(string body)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return content;
    }

    private AcmeAnswer Approved(string orderNumber, long authorisedAmount = 1999) =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "00",
            "Approved or completed successfully",
            "Approved.",
            "238699",
            authorisedAmount,
            "MR747891457",
            orderNumber,
            time.GetUtcNow());

    private AcmeAnswer Declined(string orderNumber) =>
        new(
            AcmeAnswerType.Success,
            "Acquirer",
            "51",
            "Insufficient funds",
            "There was not enough money in the account.",
            reconciliationId: "MR747891457",
            retrievalReferenceNumber: orderNumber,
            acmeTransactionAt: time.GetUtcNow());

    private SubmitPaymentResponse SendAnswered(AcmeAnswer answer) =>
        new(answer, AcmeInteractionOutcome.Answered, 200, "{}", time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private SubmitPaymentResponse SendTimedOut() =>
        new(new AcmeAnswer(AcmeAnswerType.TimedOut), AcmeInteractionOutcome.TimedOut, null, null, time.GetUtcNow(), TimeSpan.FromSeconds(35));

    private Func<GetTransactionByTransactionIdResponse> QueryAnswered(AcmeAnswer answer, string body = "{}") =>
        () => new GetTransactionByTransactionIdResponse(
            answer, AcmeInteractionOutcome.Answered, 200, body, time.GetUtcNow(), TimeSpan.FromMilliseconds(400));

    private async Task ArrangeBackgroundAsync()
    {
        database.SubMerchants.Add(new SubMerchantEntity(SubMerchantReference, "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812"));
        database.StoredCards.Add(new StoredCardEntity(
            CustomerReference,
            new CryptographyManager().Encrypt(CardToken, acceptance.TokenKey, Encoding.UTF8.GetBytes(CustomerReference)),
            EmailAddress));

        await database.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Guid> RecordPaymentAsync(string appPaymentReference)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/payments",
            new
            {
                customerReference = CustomerReference,
                subMerchantReference = SubMerchantReference,
                amount = 1999L,
                currencyCode = "ZAR",
                appPaymentReference = $"{appPaymentReference}-{Discriminator}"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // a 201 from RecordPayment always carries the payment
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;

        return body["id"]!.GetValue<Guid>();
    }

    // "which Acme approved" is arranged as Submitting Payments to Acme arranges it: the substitute answers and the
    // clock moves until the worker has sent the payment
    private async Task<Guid> RecordApprovedPaymentAsync(string appPaymentReference, long authorisedAmount = 1999)
    {
        var id = await RecordPaymentAsync(appPaymentReference);
        var orderNumber = await GetOrderNumberAsync(id);
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Approved(orderNumber, authorisedAmount)));

        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(10));
        (await GetStatusAsync(id)).ShouldBe("Approved");

        return id;
    }

    private async Task<Guid> RecordDeclinedPaymentAsync(string appPaymentReference)
    {
        var id = await RecordPaymentAsync(appPaymentReference);
        var orderNumber = await GetOrderNumberAsync(id);
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendAnswered(Declined(orderNumber)));

        await MoveClockUntilFinalAsync(id, TimeSpan.FromSeconds(10));
        (await GetStatusAsync(id)).ShouldBe("Declined");

        return id;
    }

    // "which is Unresolved" is arranged through the database with the payment's own domain methods: a send whose
    // outcome is unknown, then a query Acme refuses; the clock does not move, so the worker never claims it
    private async Task<Guid> RecordUnresolvedPaymentAsync(string appPaymentReference)
    {
        var id = await RecordPaymentAsync(appPaymentReference);
        var payment = await database.Payments
            .AsTracking()
            .SingleAsync(candidate => candidate.Id == id, TestContext.Current.CancellationToken);
        payment.RecordSendAnswer(new AcmeAnswer(AcmeAnswerType.TimedOut), null, time);
        payment.RecordQueryAnswer(
            new AcmeAnswer(AcmeAnswerType.Refused, "Application", "E02", "The API key is missing or not valid"),
            null,
            time);

        await database.SaveChangesAsync(CancellationToken.None);
        database.ChangeTracker.Clear();
        (await GetStatusAsync(id)).ShouldBe("Unresolved");

        return id;
    }

    // "Acme answered the send with a timeout": the clock moves until the worker has sent the payment, and its query
    // is answered as the test arranges
    private async Task SendTimesOutAsync(Guid id, Func<GetTransactionByTransactionIdResponse> queryAnswer)
    {
        var sends = new ConcurrentQueue<DateTimeOffset>();
        acme.SubmitPaymentAsync(SentFor(id), Arg.Any<CancellationToken>())
            .Returns(_ => SendTimedOut())
            .AndDoes(_ => sends.Enqueue(time.GetUtcNow()));
        acme.GetTransactionByTransactionIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(_ => queryAnswer());

        await acceptance.MoveClockUntilAsync(id, Second, TimeSpan.FromSeconds(10), () => Task.FromResult(!sends.IsEmpty));
        sends.ShouldHaveSingleItem();
    }

    private Task<string> GetOrderNumberAsync(Guid id) =>
        database.Payments
            .Where(payment => payment.Id == id)
            .Select(payment => payment.OrderNumber)
            .SingleAsync(TestContext.Current.CancellationToken);

    private async Task<JsonObject> GetPaymentAsync(Guid id) =>
        // the payment was recorded by this test, so the read always finds it
        (await client.GetFromJsonAsync<JsonObject>($"/v1/payments/{id}", TestContext.Current.CancellationToken))!;

    private async Task<string> GetStatusAsync(Guid id) =>
        (await GetPaymentAsync(id))["status"]!.GetValue<string>();

    private async Task<bool> IsFinalAsync(Guid id) => await GetStatusAsync(id) != "Pending";

    private Task MoveClockUntilFinalAsync(Guid id, TimeSpan limit) =>
        acceptance.MoveClockUntilAsync(id, Second, limit, () => IsFinalAsync(id));
}
