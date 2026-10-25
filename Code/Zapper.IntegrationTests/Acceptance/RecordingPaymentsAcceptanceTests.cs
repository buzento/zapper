using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Zapper.ApiCommon.Managers.Cryptography;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.Acceptance;

[Collection("Acceptance")]
public sealed class RecordingPaymentsAcceptanceTests : AcceptanceTestBase
{
    private const string TradingName = "Cafe Del Sol";

    private const string City = "Cape Town";

    private static readonly byte[] TokenKey = RandomNumberGenerator.GetBytes(32);

    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    public RecordingPaymentsAcceptanceTests(AcceptanceFixture acceptance)
    {
        client = acceptance.GetClient("Zapper.PaymentsApi");
        database = acceptance.Database;
    }

    private static string SubMerchantReference => $"ZAP{Discriminator}";

    private static string CustomerReference => $"CUS-1001-{Discriminator}";

    private static string EmailAddress => $"cus-1001-{Discriminator}@example.test";

    private static string CardToken => $"test-token-{Discriminator}";

    [Fact(DisplayName = "App records a payment for a customer with a stored card")]
    [Trait("Rule", "R-1")]
    [Trait("Rule", "R-3")]
    [Trait("Rule", "R-6")]
    public async Task AppRecordsAPaymentForACustomerWithAStoredCard()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-1-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadBodyAsync(response);
        var id = body["id"]!.GetValue<Guid>();
        body["status"]!.GetValue<string>().ShouldBe("Pending");
        body["amount"]!.GetValue<long>().ShouldBe(1999);
        body["currencyCode"]!.GetValue<string>().ShouldBe("ZAR");
        response.Headers.Location!.OriginalString.ShouldBe($"/v1/payments/{id}");
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken);
        stored.Id.ShouldBe(id);
        stored.OrderNumber.ShouldMatch("^[A-Z0-9]{12}$");
        stored.NextActionAt.ShouldBe(stored.RecordedAt);
    }

    [Fact(DisplayName = "App reads a payment it recorded")]
    [Trait("Rule", "R-8")]
    [Trait("Rule", "R-9")]
    public async Task AppReadsAPaymentItRecorded()
    {
        await ArrangeBackgroundAsync();

        var appPaymentReference = $"APP-2-{Discriminator}";
        var recorded = await RecordPaymentAsync(new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference
        });

        var response = await client.GetAsync(
            $"/v1/payments/{recorded["id"]!.GetValue<Guid>()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body["status"]!.GetValue<string>().ShouldBe("Pending");
        body["amount"]!.GetValue<long>().ShouldBe(1999);
        body["currencyCode"]!.GetValue<string>().ShouldBe("ZAR");
        body["appPaymentReference"]!.GetValue<string>().ShouldBe(appPaymentReference);
        body["authorisedAmount"].ShouldBeNull();
        body["responseCode"].ShouldBeNull();
        body["clientMessage"].ShouldBeNull();
        body["reversedAmount"]!.GetValue<long>().ShouldBe(0);
        body["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact(DisplayName = "Payment of less than one cent is refused")]
    [Trait("Rule", "R-2")]
    public async Task PaymentOfLessThanOneCentIsRefused()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 0L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-3-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["errors"]!.AsObject().Select(field => field.Key).ShouldBe(["amount"]);
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "Payment of more than one million rand is refused")]
    [Trait("Rule", "R-2")]
    public async Task PaymentOfMoreThanOneMillionRandIsRefused()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 100000001L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-4-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("AmountAboveMaximum");
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "Payment in a currency that is not supported is refused")]
    [Trait("Rule", "R-3")]
    public async Task PaymentInACurrencyThatIsNotSupportedIsRefused()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "JPY",
            appPaymentReference = $"APP-5-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("CurrencyCodeNotSupported");
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "Currency code in lower case is accepted")]
    [Trait("Rule", "R-3")]
    public async Task CurrencyCodeInLowerCaseIsAccepted()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "usd",
            appPaymentReference = $"APP-6-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadBodyAsync(response);
        body["currencyCode"]!.GetValue<string>().ShouldBe("USD");
    }

    [Fact(DisplayName = "Payment without a currency is recorded in rand")]
    [Trait("Rule", "R-3")]
    public async Task PaymentWithoutACurrencyIsRecordedInRand()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            appPaymentReference = $"APP-14-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadBodyAsync(response);
        body["currencyCode"]!.GetValue<string>().ShouldBe("ZAR");
    }

    [Fact(DisplayName = "Payment for a customer without a stored card is refused")]
    [Trait("Rule", "R-4")]
    public async Task PaymentForACustomerWithoutAStoredCardIsRefused()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = "CUS-9999",
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-7-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("CustomerHasNoStoredCard");
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "Payment at an unregistered sub-merchant is refused")]
    [Trait("Rule", "R-4")]
    public async Task PaymentAtAnUnregisteredSubMerchantIsRefused()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = "ZAP9999999",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-8-{Discriminator}"
        };

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("SubMerchantNotRegistered");
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact(DisplayName = "Same payment recorded twice returns the first payment")]
    [Trait("Rule", "R-5")]
    public async Task SamePaymentRecordedTwiceReturnsTheFirstPayment()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-9-{Discriminator}"
        };
        var first = await PostPaymentAsync(request);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstBody = await ReadBodyAsync(first);

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadBodyAsync(response);
        body["id"]!.GetValue<Guid>().ShouldBe(firstBody["id"]!.GetValue<Guid>());
        JsonNode.DeepEquals(body, firstBody).ShouldBeTrue();
        response.Headers.Location.ShouldBe(first.Headers.Location);
        (await database.Payments.CountAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact(DisplayName = "Reused app reference with a different amount is refused")]
    [Trait("Rule", "R-5")]
    public async Task ReusedAppReferenceWithADifferentAmountIsRefused()
    {
        await ArrangeBackgroundAsync();

        var appPaymentReference = $"APP-10-{Discriminator}";
        await RecordPaymentAsync(new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference
        });

        var response = await PostPaymentAsync(new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 2500L,
            currencyCode = "ZAR",
            appPaymentReference
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("AppPaymentReferenceReused");
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == appPaymentReference, TestContext.Current.CancellationToken);
        stored.Amount.ShouldBe(1999);
    }

    [Fact(DisplayName = "Two identical requests at the same moment record one payment")]
    [Trait("Rule", "R-5")]
    public async Task TwoIdenticalRequestsAtTheSameMomentRecordOnePayment()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-11-{Discriminator}"
        };

        var responses = await Task.WhenAll(PostPaymentAsync(request), PostPaymentAsync(request));

        responses.Select(response => response.StatusCode)
            .ShouldBe([HttpStatusCode.Created, HttpStatusCode.Created]);
        var bodies = await Task.WhenAll(responses.Select(ReadBodyAsync));
        bodies[1]["id"]!.GetValue<Guid>().ShouldBe(bodies[0]["id"]!.GetValue<Guid>());
        (await database.Payments.CountAsync(
                payment => payment.AppPaymentReference == request.appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact(DisplayName = "Card acceptor name follows the FNB layout")]
    [Trait("Rule", "R-7")]
    public async Task CardAcceptorNameFollowsTheFnbLayout()
    {
        await ArrangeBackgroundAsync();

        var request = new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-12-{Discriminator}"
        };

        var recorded = await RecordPaymentAsync(request);

        var id = recorded["id"]!.GetValue<Guid>();
        var stored = await database.Payments.SingleAsync(
            payment => payment.Id == id, TestContext.Current.CancellationToken);
        stored.CardAcceptorName.ShouldBe("ZAP*CAFE DEL SOL       CAPE TOWN    WCZA");
        stored.MerchantCategoryCode.ShouldBe("5812");
    }

    [Fact(DisplayName = "Reading an unknown payment answers not found")]
    [Trait("Rule", "R-8")]
    public async Task ReadingAnUnknownPaymentAnswersNotFound()
    {
        await ArrangeBackgroundAsync();

        var id = Guid.CreateVersion7();

        var response = await client.GetAsync($"/v1/payments/{id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await ReadBodyAsync(response);
        body["code"]!.GetValue<string>().ShouldBe("NotFound");
    }

    [Fact(DisplayName = "Payment answer carries no personal information")]
    [Trait("Rule", "R-8")]
    [Trait("Rule", "R-10")]
    public async Task PaymentAnswerCarriesNoPersonalInformation()
    {
        await ArrangeBackgroundAsync();

        var recorded = await RecordPaymentAsync(new
        {
            customerReference = CustomerReference,
            subMerchantReference = SubMerchantReference,
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = $"APP-13-{Discriminator}"
        });

        var response = await client.GetAsync(
            $"/v1/payments/{recorded["id"]!.GetValue<Guid>()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.Select(member => member.Key).ShouldBe(
            [
                "id", "appPaymentReference", "status", "amount", "currencyCode", "authorisedAmount",
                "responseCode", "clientMessage", "reversedAmount", "refundedAmount"
            ],
            ignoreOrder: true);
        var answer = body.ToJsonString();
        answer.ShouldNotContain(CustomerReference);
        answer.ShouldNotContain(EmailAddress);
        answer.ShouldNotContain(CardToken);
        answer.ShouldNotContain(SubMerchantReference);
        answer.ShouldNotContain(TradingName);
        answer.ShouldNotContain(City);
    }

    private async Task ArrangeBackgroundAsync()
    {
        database.SubMerchants.Add(new SubMerchantEntity(SubMerchantReference, TradingName, City, "WC", "ZA", "5812"));
        database.StoredCards.Add(new StoredCardEntity(
            CustomerReference,
            new CryptographyManager().Encrypt(CardToken, TokenKey, Encoding.UTF8.GetBytes(CustomerReference)),
            EmailAddress));

        await database.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<JsonObject> ReadBodyAsync(HttpResponseMessage response) =>
        // every answer this Feature names carries a JSON object, a ProblemDetails for a failure included
        (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;

    private Task<HttpResponseMessage> PostPaymentAsync(object request) =>
        client.PostAsJsonAsync("/v1/payments", request, TestContext.Current.CancellationToken);

    private async Task<JsonObject> RecordPaymentAsync(object request)
    {
        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return await ReadBodyAsync(response);
    }
}
