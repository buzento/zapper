using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using Zapper.ApiCommon.Errors;
using Zapper.Domain.Entities.Customer;
using Zapper.Domain.Entities.Merchant;
using Zapper.Domain.Entities.Payment;
using Zapper.Domain.Enums;
using Zapper.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Zapper.IntegrationTests.PaymentsApi.Features.Payments;

public sealed class GetPaymentIntegrationTests : IClassFixture<PaymentsApiFixture>
{
    private readonly HttpClient client;

    private readonly ZapperDbContext database;

    public GetPaymentIntegrationTests(PaymentsApiFixture api)
    {
        client = api.CreateClient();
        database = api.Database;
    }

    [Fact]
    public async Task GetPayment_StoredPayment_ReturnsItsStatusAndAmounts()
    {
        var payment = await SavePaymentAsync("0401", Currency.Eur, 2500);

        var response = await client.GetAsync($"/v1/payments/{payment.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["id"]!.GetValue<Guid>().ShouldBe(payment.Id);
        body["appPaymentReference"]!.GetValue<string>().ShouldBe("APP-0401");
        body["status"]!.GetValue<string>().ShouldBe("Pending");
        body["amount"]!.GetValue<long>().ShouldBe(2500);
        body["currencyCode"]!.GetValue<string>().ShouldBe("EUR");
        body["authorisedAmount"].ShouldBeNull();
        body["responseCode"].ShouldBeNull();
        body["clientMessage"].ShouldBeNull();
        body["reversedAmount"]!.GetValue<long>().ShouldBe(0);
        body["refundedAmount"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact]
    public async Task GetPayment_TwoStoredPayments_ReturnsOnlyTheOneNamed()
    {
        await SavePaymentAsync("0402", Currency.Zar, 1999);
        var payment = await SavePaymentAsync("0403", Currency.Zar, 3000);

        var response = await client.GetAsync($"/v1/payments/{payment.Id}", TestContext.Current.CancellationToken);

        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["appPaymentReference"]!.GetValue<string>().ShouldBe("APP-0403");
        body["amount"]!.GetValue<long>().ShouldBe(3000);
    }

    [Fact]
    public async Task GetPayment_UnknownId_ReturnsNotFound()
    {
        var id = Guid.CreateVersion7();

        var response = await client.GetAsync($"/v1/payments/{id}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        body["code"]!.GetValue<string>().ShouldBe(ErrorMessageCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_MalformedId_ReturnsNotFound()
    {
        var response = await client.GetAsync("/v1/payments/not-an-id", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<PaymentEntity> SavePaymentAsync(string number, Currency currencyId, long amount)
    {
        var storedCard = new StoredCardEntity($"CUS-{number}", [1, 2, 3], $"cus-{number}@example.test");
        var subMerchant = new SubMerchantEntity($"ZAP000{number}", "Cafe Del Sol", "Cape Town", "WC", "ZA", "5812");
        var currency = await database.CurrencyTypes.SingleAsync(
            candidate => candidate.Id == currencyId, TestContext.Current.CancellationToken);
        var payment = new PaymentEntity(storedCard, subMerchant, currency, amount, $"APP-{number}", new FakeTimeProvider());
        database.StoredCards.Add(storedCard);
        database.SubMerchants.Add(subMerchant);
        database.Payments.Add(payment);
        await database.SaveChangesAsync(CancellationToken.None);

        return payment;
    }
}
