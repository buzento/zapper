using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Zapper.ApiCommon.Errors;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Enums;
using Zapper.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.PaymentsApi.Features.Payments;

public sealed class RecordPaymentIntegrationTests : IClassFixture<PaymentsApiFixture>
{
    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    public RecordPaymentIntegrationTests(PaymentsApiFixture api)
    {
        client = api.CreateClient();
        database = api.Database;
    }

    [Fact]
    public async Task RecordPayment_CustomerWithStoredCard_StoresPaymentAndReturnsCreated()
    {
        var (storedCard, subMerchant) = await SaveCustomerAndSubMerchantAsync("0301");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0301",
            subMerchantReference = "ZAP0000301",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0301"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ReadBodyAsync(response);
        var id = body["id"]!.GetValue<Guid>();
        response.Headers.Location!.OriginalString.ShouldBe($"/v1/payments/{id}");
        body["status"]!.GetValue<string>().ShouldBe("Pending");
        var stored = await database.Payments.SingleAsync(
            payment => payment.Id == id, TestContext.Current.CancellationToken);
        stored.AppPaymentReference.ShouldBe("APP-0301");
        stored.StoredCardId.ShouldBe(storedCard.Id);
        stored.SubMerchantId.ShouldBe(subMerchant.Id);
        stored.Currency.ShouldBe(Currency.Zar);
        stored.Amount.ShouldBe(1999);
        stored.State.ShouldBe(PaymentState.Recorded);
    }

    [Fact]
    public async Task RecordPayment_CurrencyInLowerCase_StoresAndAnswersItInCapitals()
    {
        await SaveCustomerAndSubMerchantAsync("0302");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0302",
            subMerchantReference = "ZAP0000302",
            amount = 1999L,
            currencyCode = "gbp",
            appPaymentReference = "APP-0302"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ReadBodyAsync(response))["currencyCode"]!.GetValue<string>().ShouldBe("GBP");
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == "APP-0302", TestContext.Current.CancellationToken);
        stored.Currency.ShouldBe(Currency.Gbp);
    }

    [Fact]
    public async Task RecordPayment_NoCurrency_StoresPaymentInRand()
    {
        await SaveCustomerAndSubMerchantAsync("0303");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0303",
            subMerchantReference = "ZAP0000303",
            amount = 1999L,
            appPaymentReference = "APP-0303"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == "APP-0303", TestContext.Current.CancellationToken);
        stored.Currency.ShouldBe(Currency.Zar);
    }

    [Fact]
    public async Task RecordPayment_SameRequestAgain_ReturnsHeldPayment()
    {
        await SaveCustomerAndSubMerchantAsync("0304");
        var request = new
        {
            customerReference = "CUS-0304",
            subMerchantReference = "ZAP0000304",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0304"
        };
        var first = await ReadBodyAsync(await PostPaymentAsync(request));

        var response = await PostPaymentAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        JsonNode.DeepEquals(await ReadBodyAsync(response), first).ShouldBeTrue();
        (await database.Payments.CountAsync(
                payment => payment.AppPaymentReference == "APP-0304", TestContext.Current.CancellationToken))
            .ShouldBe(1);
    }

    [Fact]
    public async Task RecordPayment_TwoIdenticalRequestsAtOnce_RecordsOnePayment()
    {
        await SaveCustomerAndSubMerchantAsync("0305");
        var request = new
        {
            customerReference = "CUS-0305",
            subMerchantReference = "ZAP0000305",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0305"
        };

        var responses = await Task.WhenAll(PostPaymentAsync(request), PostPaymentAsync(request));

        responses.Select(response => response.StatusCode).ShouldBe([HttpStatusCode.Created, HttpStatusCode.Created]);
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == "APP-0305", TestContext.Current.CancellationToken);
        var bodies = await Task.WhenAll(responses.Select(ReadBodyAsync));
        bodies.Select(body => body["id"]!.GetValue<Guid>()).ShouldBe([stored.Id, stored.Id]);
    }

    [Fact]
    public async Task RecordPayment_HeldReferenceWithDifferentAmount_ReturnsAppPaymentReferenceReused()
    {
        await SaveCustomerAndSubMerchantAsync("0306");
        await PostPaymentAsync(new
        {
            customerReference = "CUS-0306",
            subMerchantReference = "ZAP0000306",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0306"
        });

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0306",
            subMerchantReference = "ZAP0000306",
            amount = 2500L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0306"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.AppPaymentReferenceReused);
        var stored = await database.Payments.SingleAsync(
            payment => payment.AppPaymentReference == "APP-0306", TestContext.Current.CancellationToken);
        stored.Amount.ShouldBe(1999);
    }

    [Fact]
    public async Task RecordPayment_HeldReferenceWithUnknownCustomer_ReturnsAppPaymentReferenceReusedFirst()
    {
        await SaveCustomerAndSubMerchantAsync("0307");
        await PostPaymentAsync(new
        {
            customerReference = "CUS-0307",
            subMerchantReference = "ZAP0000307",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0307"
        });

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0307-UNKNOWN",
            subMerchantReference = "ZAP0000307",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0307"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.AppPaymentReferenceReused);
    }

    [Fact]
    public async Task RecordPayment_CustomerWithoutStoredCard_ReturnsCustomerHasNoStoredCard()
    {
        await SaveCustomerAndSubMerchantAsync("0308");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0308-UNKNOWN",
            subMerchantReference = "ZAP0000308",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0308"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.CustomerHasNoStoredCard);
        await ShouldNotBeRecordedAsync("APP-0308");
    }

    [Fact]
    public async Task RecordPayment_UnregisteredSubMerchant_ReturnsSubMerchantNotRegistered()
    {
        await SaveCustomerAndSubMerchantAsync("0309");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0309",
            subMerchantReference = "ZAP9999309",
            amount = 1999L,
            currencyCode = "ZAR",
            appPaymentReference = "APP-0309"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.SubMerchantNotRegistered);
        await ShouldNotBeRecordedAsync("APP-0309");
    }

    [Fact]
    public async Task RecordPayment_CurrencyNotListed_ReturnsCurrencyCodeNotSupported()
    {
        await SaveCustomerAndSubMerchantAsync("0310");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0310",
            subMerchantReference = "ZAP0000310",
            amount = 1999L,
            currencyCode = "JPY",
            appPaymentReference = "APP-0310"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.CurrencyCodeNotSupported);
        await ShouldNotBeRecordedAsync("APP-0310");
    }

    [Fact]
    public async Task RecordPayment_AmountAboveCurrencyMaximum_ReturnsAmountAboveMaximum()
    {
        await SaveCustomerAndSubMerchantAsync("0311");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0311",
            subMerchantReference = "ZAP0000311",
            amount = 5000001L,
            currencyCode = "USD",
            appPaymentReference = "APP-0311"
        });

        await ShouldBeRefusedAsync(response, ErrorMessageCode.AmountAboveMaximum);
        await ShouldNotBeRecordedAsync("APP-0311");
    }

    [Fact]
    public async Task RecordPayment_AmountAtCurrencyMaximum_ReturnsCreated()
    {
        await SaveCustomerAndSubMerchantAsync("0312");

        var response = await PostPaymentAsync(new
        {
            customerReference = "CUS-0312",
            subMerchantReference = "ZAP0000312",
            amount = 5000000L,
            currencyCode = "USD",
            appPaymentReference = "APP-0312"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("", "ZAP0000313", 1999L, "ZAR", "APP-0313", "customerReference")]
    [InlineData("CUS-012345678901234567890123456789012345678901234567", "ZAP0000313", 1999L, "ZAR", "APP-0313", "customerReference")]
    [InlineData("CUS-0313", "", 1999L, "ZAR", "APP-0313", "subMerchantReference")]
    [InlineData("CUS-0313", "ZAP0000000000313", 1999L, "ZAR", "APP-0313", "subMerchantReference")]
    [InlineData("CUS-0313", "ZAP0000313", 0L, "ZAR", "APP-0313", "amount")]
    [InlineData("CUS-0313", "ZAP0000313", 1999L, "ZARX", "APP-0313", "currencyCode")]
    [InlineData("CUS-0313", "ZAP0000313", 1999L, "ZAR", "", "appPaymentReference")]
    [InlineData("CUS-0313", "ZAP0000313", 1999L, "ZAR", "APP-012345678901234567890123456789012345678901234567", "appPaymentReference")]
    public async Task RecordPayment_FieldBreaksItsRule_ReturnsValidationProblemForThatField(
        string customerReference,
        string subMerchantReference,
        long amount,
        string currencyCode,
        string appPaymentReference,
        string field)
    {
        var response = await PostPaymentAsync(new
        {
            customerReference,
            subMerchantReference,
            amount,
            currencyCode,
            appPaymentReference
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadBodyAsync(response))["errors"]!.AsObject().Select(error => error.Key).ShouldBe([field]);
    }

    [Fact]
    public async Task RecordPayment_BodyNotJson_ReturnsBadRequest()
    {
        using var content = new StringContent("not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/v1/payments", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadBodyAsync(response))["code"]!.GetValue<string>().ShouldBe(ErrorMessageCode.BadRequest);
    }

    private static async Task<JsonObject> ReadBodyAsync(HttpResponseMessage response) =>
        // every answer this operation gives carries a JSON object, a ProblemDetails for a failure included
        (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;

    private static async Task ShouldBeRefusedAsync(HttpResponseMessage response, string code)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadBodyAsync(response))["code"]!.GetValue<string>().ShouldBe(code);
    }

    private Task<HttpResponseMessage> PostPaymentAsync(object request) =>
        client.PostAsJsonAsync("/v1/payments", request, TestContext.Current.CancellationToken);

    private async Task ShouldNotBeRecordedAsync(string appPaymentReference) =>
        (await database.Payments.AnyAsync(
                payment => payment.AppPaymentReference == appPaymentReference, TestContext.Current.CancellationToken))
            .ShouldBeFalse();

    private async Task<(StoredCardEntity StoredCard, SubMerchantEntity SubMerchant)> SaveCustomerAndSubMerchantAsync(string number)
    {
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        await database.SaveChangesAsync(CancellationToken.None);

        return (storedCard, subMerchant);
    }
}
